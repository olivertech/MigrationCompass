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

// Centraliza a leitura dos argumentos e delega a execuÃ§Ã£o para o pipeline principal.
rootCommand.SetAction(async parseResult =>
{
    var solutionFile = parseResult.GetValue(solutionOption);
    var outputDirectory = parseResult.GetValue(outputOption);
    var format = parseResult.GetValue(formatOption) ?? "html";
    return await RunAsync(solutionFile, outputDirectory, format);
});

return await rootCommand.Parse(args).InvokeAsync();

// Orquestra o scan completo da solution e a geraÃ§Ã£o do relatÃ³rio final.
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
    var scanResult = await solutionScanner.ScanAsync(request, CancellationToken.None);
    scanResult.EconomicParameters = economicParameters;
    scanResult.ApiFindings.AddRange(await apiScanner.ScanAsync(scanResult.Projects, CancellationToken.None));
    scanResult.SolidFindings.AddRange(await solidScanner.ScanAsync(scanResult.Projects, CancellationToken.None));
    scanResult.PackageFindings.AddRange(await nugetChecker.CheckAsync(scanResult.Projects, CancellationToken.None));
    scanResult.Summary = ReportSummaryBuilder.Build(scanResult);
    scanResult.Advisory = StrategyAdvisor.Build(scanResult);

    Directory.CreateDirectory(resolvedOutputDirectory);
    var reportFilePath = reportGenerator.Write(scanResult, resolvedOutputDirectory);

    Console.WriteLine($"Solution analisada: {Path.GetFileName(scanResult.SolutionPath)}");
    Console.WriteLine($"Projetos escaneados: {scanResult.Projects.Count}");
    Console.WriteLine($"Bloqueadores criticos: {scanResult.Summary.CriticalBlockers}");
    Console.WriteLine($"Avisos: {scanResult.Summary.Warnings}");
    Console.WriteLine($"Relatorio gerado em: {reportFilePath}");
    return 0;
}

// Resolve a solution de entrada a partir do parÃ¢metro explÃ­cito ou da auto descoberta local.
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
