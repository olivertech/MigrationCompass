using System.CommandLine;
using MigrationCompass.Models;
using MigrationCompass.Reporting;
using MigrationCompass.Services;

// Define a interface de linha de comando exposta ao operador do scanner.
var solutionOption = new Option<FileInfo?>("--sln")
{
    Description = "Caminho para o arquivo .sln (obrigatorio)."
};

var outputOption = new Option<DirectoryInfo?>("--output")
{
    Description = "Diretorio para saida do relatorio (padrao: ./)."
};

var formatOption = new Option<string>("--format")
{
    Description = "Formato do relatorio: html (apenas)."
};

var rootCommand = new RootCommand("MigrationCompass")
{
    solutionOption,
    outputOption,
    formatOption
};

// Centraliza a leitura dos argumentos e delega a execução para o pipeline principal.
rootCommand.SetAction(async parseResult =>
{
    var solutionFile = parseResult.GetValue(solutionOption);
    var outputDirectory = parseResult.GetValue(outputOption);
    var format = parseResult.GetValue(formatOption) ?? "html";
    return await RunAsync(solutionFile, outputDirectory, format);
});

return await rootCommand.Parse(args).InvokeAsync();

// Orquestra o scan completo da solution e a geração do relatório final.
static async Task<int> RunAsync(FileInfo? solutionFile, DirectoryInfo? outputDirectory, string format)
{
    if (!string.Equals(format, "html", StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Erro: Formato de relatorio invalido. Apenas 'html' e suportado.");
        return 1;
    }

    var resolvedSolutionPath = ResolveSolutionPath(solutionFile);
    if (resolvedSolutionPath is null)
    {
        return 1;
    }

    if (!File.Exists(resolvedSolutionPath))
    {
        Console.Error.WriteLine("Erro: Arquivo de solution nao encontrado");
        return 1;
    }

    var resolvedOutputDirectory = outputDirectory?.FullName ?? Directory.GetCurrentDirectory();

    var rulesPath = Path.Combine(AppContext.BaseDirectory, "Rules", "BlockingRules.json");
    var economicParametersPath = Path.Combine(AppContext.BaseDirectory, "Rules", "EconomicParameters.json");
    var irrelevantPackagesPath = Path.Combine(AppContext.BaseDirectory, "Rules", "IrrelevantPackages.json");
    var rules = await RuleCatalog.LoadAsync(rulesPath, CancellationToken.None);
    var economicParameters = await EconomicParametersCatalog.LoadAsync(economicParametersPath, CancellationToken.None);
    var irrelevantPackages = await IrrelevantPackageCatalog.LoadAsync(irrelevantPackagesPath, CancellationToken.None);
    var solutionScanner = new SolutionScanner();
    var apiScanner = new ApiScanner(rules);
    var solidScanner = new SolidScanner();
    var nugetClient = new FlatContainerNuGetClient(new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(20)
    });
    var nugetChecker = new NuGetChecker(nugetClient, rules, irrelevantPackages);
    var reportGenerator = new HtmlReportGenerator();

    var request = new ScanRequest(resolvedSolutionPath, resolvedOutputDirectory, format);

    WriteStep("Iniciando analise da solution...");
    var scanResult = await ExecuteWithProgressAsync(
        "Descobrindo solution e projetos",
        () => solutionScanner.ScanAsync(request, CancellationToken.None));

    scanResult.EconomicParameters = economicParameters;

    WriteStep("Mapeando APIs legadas...");
    scanResult.ApiFindings.AddRange(await ExecuteWithProgressAsync(
        "Analisando uso de APIs legadas",
        () => apiScanner.ScanAsync(scanResult.Projects, CancellationToken.None)));

    WriteStep("Inspecionando sinais estruturais do codigo...");
    scanResult.SolidFindings.AddRange(await ExecuteWithProgressAsync(
        "Procurando sinais heurísticos de SOLID",
        () => solidScanner.ScanAsync(scanResult.Projects, CancellationToken.None)));

    WriteStep("Validando dependencias NuGet...");
    scanResult.PackageFindings.AddRange(await ExecuteWithProgressAsync(
        "Checando compatibilidade de pacotes",
        () => nugetChecker.CheckAsync(scanResult.Projects, CancellationToken.None)));

    WriteStep("Consolidando pontuacoes e recomendacoes...");
    scanResult.Summary = ReportSummaryBuilder.Build(scanResult);
    scanResult.Advisory = StrategyAdvisor.Build(scanResult);

    WriteStep("Gerando relatorio HTML...");
    Directory.CreateDirectory(resolvedOutputDirectory);
    var reportFilePath = await ExecuteWithProgressAsync(
        "Escrevendo relatorio final",
        () => Task.FromResult(reportGenerator.Write(scanResult, resolvedOutputDirectory)));

    WriteSuccess("Analise concluida com sucesso.");
    Console.WriteLine($"Solution analisada: {Path.GetFileName(scanResult.SolutionPath)}");
    Console.WriteLine($"Projetos escaneados: {scanResult.Projects.Count}");
    Console.WriteLine($"Bloqueadores criticos: {scanResult.Summary.CriticalBlockers}");
    Console.WriteLine($"Avisos: {scanResult.Summary.Warnings}");
    Console.WriteLine($"Relatorio gerado em: {reportFilePath}");
    return 0;
}

// Resolve a solution de entrada a partir do parâmetro explícito ou da auto descoberta local.
static string? ResolveSolutionPath(FileInfo? solutionFile)
{
    if (solutionFile is not null)
    {
        return solutionFile.FullName;
    }

    var currentDirectory = Directory.GetCurrentDirectory();
    var solutionFiles = Directory.GetFiles(currentDirectory, "*.sln", SearchOption.TopDirectoryOnly);
    if (solutionFiles.Length == 1)
    {
        return solutionFiles[0];
    }

    if (solutionFiles.Length == 0)
    {
        Console.Error.WriteLine("Erro: Nenhum arquivo .sln encontrado no diretorio atual. Informe --sln.");
        return null;
    }

    Console.Error.WriteLine("Erro: Multiplas solutions encontradas no diretorio atual. Informe --sln explicitamente.");
    return null;
}

static async Task<T> ExecuteWithProgressAsync<T>(string message, Func<Task<T>> action)
{
    if (Console.IsOutputRedirected)
    {
        Console.WriteLine($"{message}...");
        return await action();
    }

    using var progress = new ConsoleProgress(message);
    return await action();
}

static void WriteStep(string message)
{
    Console.WriteLine();
    Console.WriteLine($"> {message}");
}

static void WriteSuccess(string message)
{
    Console.WriteLine();
    Console.WriteLine($"OK  {message}");
}

sealed class ConsoleProgress : IDisposable
{
    private static readonly char[] Frames = ['|', '/', '-', '\\'];
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _renderTask;
    private readonly string _message;
    private bool _disposed;

    public ConsoleProgress(string message)
    {
        _message = message;
        _renderTask = Task.Run(RenderAsync);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();

        try
        {
            _renderTask.Wait();
        }
        catch (AggregateException exception) when (exception.InnerExceptions.All(inner => inner is TaskCanceledException))
        {
        }
        catch (OperationCanceledException)
        {
        }

        ClearCurrentLine();
        Console.WriteLine($"OK  {_message}");
        _cancellation.Dispose();
    }

    private async Task RenderAsync()
    {
        var index = 0;
        while (!_cancellation.IsCancellationRequested)
        {
            Console.Write($"\r{Frames[index]} {_message}...");
            index = (index + 1) % Frames.Length;
            await Task.Delay(120, _cancellation.Token);
        }
    }

    private static void ClearCurrentLine()
    {
        var width = Math.Max(Console.WindowWidth - 1, 20);
        Console.Write("\r" + new string(' ', width) + "\r");
    }
}
