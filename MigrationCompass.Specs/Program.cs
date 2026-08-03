using MigrationCompass.Models;
using MigrationCompass.Reporting;
using MigrationCompass.Services;

var suite = new SpecSuite();
var failures = await suite.RunAsync();
return failures;

internal sealed class SpecSuite
{
    private readonly string _root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public async Task<int> RunAsync()
    {
        var failures = new List<string>();
        await RunAsync("Classifica TFMs", TestFrameworkClassificationAsync, failures);
        await RunAsync("Le solution fixture", TestSolutionScannerAsync, failures);
        await RunAsync("Detecta APIs legadas", TestApiScannerAsync, failures);
        await RunAsync("Calcula score capped", TestRiskScoreAsync, failures);
        await RunAsync("Gera HTML esperado", TestHtmlGeneratorAsync, failures);
        await RunAsync("NuGet offline vira aviso", TestNuGetOfflineFallbackAsync, failures);

        if (failures.Count == 0)
        {
            Console.WriteLine("Todos os testes passaram.");
            return 0;
        }

        Console.Error.WriteLine("Falhas encontradas:");
        foreach (var failure in failures)
        {
            Console.Error.WriteLine($" - {failure}");
        }

        return 1;
    }

    private static Task TestFrameworkClassificationAsync()
    {
        AssertEqual(".NET Framework 3.x-4.x", ProjectClassification.Classify(["net48"]).Classification, "net48");
        AssertEqual(".NET Framework 3.x-4.x", ProjectClassification.Classify(["net35"]).Classification, "net35");
        AssertEqual(".NET Core 2.x/3.x", ProjectClassification.Classify(["netcoreapp3.1"]).Classification, "netcoreapp3.1");
        AssertEqual(".NET 5-7", ProjectClassification.Classify(["net6.0"]).Classification, "net6.0");
        AssertEqual(".NET 8-9", ProjectClassification.Classify(["net8.0"]).Classification, "net8.0");
        AssertEqual(".NET 8-9", ProjectClassification.Classify(["net9.0"]).Classification, "net9.0");
        return Task.CompletedTask;
    }

    private async Task TestSolutionScannerAsync()
    {
        var scanner = new SolutionScanner();
        var solutionPath = Path.Combine(_root, "Fixtures", "SampleLegacySolution", "SampleLegacySolution.sln");
        var result = await scanner.ScanAsync(new ScanRequest(solutionPath, _root, "html"), CancellationToken.None);

        AssertEqual(3, result.Projects.Count, "Quantidade de projetos");
        AssertTrue(result.Projects.Any(project => project.TargetFrameworks.Contains("net48")), "Projeto net48");
        AssertTrue(result.Projects.Any(project => project.TargetFrameworks.Contains("net9.0")), "Projeto net9.0");
        AssertTrue(result.Projects.Any(project => project.PackageReferences.Any(packageReference => packageReference.PackageId == "Elmah")), "Pacote vindo de packages.config");
    }

    private async Task TestApiScannerAsync()
    {
        var rulesPath = Path.Combine(_root, "Rules", "BlockingRules.json");
        var rules = await RuleCatalog.LoadAsync(rulesPath, CancellationToken.None);
        var scanner = new SolutionScanner();
        var solutionPath = Path.Combine(_root, "Fixtures", "SampleLegacySolution", "SampleLegacySolution.sln");
        var result = await scanner.ScanAsync(new ScanRequest(solutionPath, _root, "html"), CancellationToken.None);
        var apiScanner = new ApiScanner(rules);
        var findings = await apiScanner.ScanAsync(result.Projects, CancellationToken.None);

        AssertTrue(findings.Any(finding => finding.Rule.Id == "WEB001"), "Detecta HttpContext.Current");
        AssertTrue(findings.Any(finding => finding.Rule.Id == "SEC006"), "Detecta FormsAuthentication");
    }

    private static Task TestRiskScoreAsync()
    {
        var result = new SolutionScanResult
        {
            SolutionName = "Demo",
            SolutionPath = "Demo.sln"
        };

        result.Projects.Add(new ProjectScanResult
        {
            ProjectName = "A",
            ProjectPath = "A.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences = [],
            AssemblyReferences = [],
            SourceFiles = []
        });

        result.Projects.Add(new ProjectScanResult
        {
            ProjectName = "B",
            ProjectPath = "B.csproj",
            TargetFrameworks = ["net6.0"],
            MigrationProfile = ProjectClassification.Classify(["net6.0"]),
            PackageReferences = [],
            AssemblyReferences = [],
            SourceFiles = []
        });

        result.ApiFindings.Add(new ApiFinding
        {
            ProjectName = "A",
            FilePath = "a.cs",
            LineNumber = 1,
            MatchedText = "HttpContext.Current",
            Rule = new ApiRule
            {
                Id = "WEB001",
                Api = "System.Web.HttpContext.Current",
                Category = "WebForms",
                Impact = "Alto",
                Effort = "Medio",
                Alternative = "Use IHttpContextAccessor",
                Docs = "https://learn.microsoft.com/"
            }
        });

        result.PackageFindings.Add(new PackageCompatibilityFinding
        {
            ProjectName = "B",
            PackageId = "Legacy.Package",
            RequestedVersion = "1.0.0",
            Status = "Compativel com atualizacao",
            Impact = "Medio",
            Recommendation = "Atualizar",
            Details = "Detalhes",
            IsBlocker = false,
            IsWarning = true
        });

        result.Summary = ReportSummaryBuilder.Build(result);
        AssertEqual(90, result.Summary.RiskScore, "Score de risco");
        return Task.CompletedTask;
    }

    private static Task TestHtmlGeneratorAsync()
    {
        var result = new SolutionScanResult
        {
            SolutionName = "MinhaSolucao",
            SolutionPath = "C:\\Temp\\MinhaSolucao.sln",
            Summary = new ReportSummary
            {
                ProjectsScanned = 1,
                CriticalBlockers = 1,
                Warnings = 1,
                InformationalItems = 1,
                RiskScore = 100
            }
        };

        result.Projects.Add(new ProjectScanResult
        {
            ProjectName = "Legacy.Web",
            ProjectPath = "Legacy.Web.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences = [],
            AssemblyReferences = [],
            SourceFiles = []
        });

        result.ApiFindings.Add(new ApiFinding
        {
            ProjectName = "Legacy.Web",
            FilePath = "Legacy.cs",
            LineNumber = 1,
            MatchedText = "HttpContext.Current",
            Rule = new ApiRule
            {
                Id = "WEB001",
                Api = "System.Web.HttpContext.Current",
                Category = "WebForms",
                Impact = "Alto",
                Effort = "Medio",
                Alternative = "Use IHttpContextAccessor",
                Docs = "https://learn.microsoft.com/"
            }
        });

        var html = new HtmlReportGenerator().Generate(result);
        AssertTrue(html.Contains("Relatorio de Compatibilidade de Migracao para .NET 10 LTS", StringComparison.Ordinal), "Cabecalho");
        AssertTrue(html.Contains("Pontuacao de Risco: 100/100", StringComparison.Ordinal), "Score");
        AssertTrue(html.Contains("System.Web.HttpContext.Current (WEB001)", StringComparison.Ordinal), "Tabela de bloqueadores");
        return Task.CompletedTask;
    }

    private static async Task TestNuGetOfflineFallbackAsync()
    {
        var checker = new NuGetChecker(new FakeNuGetPackageClient());
        var project = new ProjectScanResult
        {
            ProjectName = "Offline.Project",
            ProjectPath = "Offline.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences = [new PackageReferenceInfo { PackageId = "Elmah", Version = "1.2.2", PrivateAssets = null, IsFromPackagesConfig = false }],
            AssemblyReferences = [],
            SourceFiles = []
        };

        var findings = await checker.CheckAsync([project], CancellationToken.None);
        AssertEqual("Nao verificado offline", findings.Single().Status, "Fallback offline");
    }

    private static async Task RunAsync(string name, Func<Task> test, List<string> failures)
    {
        try
        {
            await test();
            Console.WriteLine($"[OK] {name}");
        }
        catch (Exception ex)
        {
            failures.Add($"{name}: {ex.Message}");
        }
    }

    private static void AssertTrue(bool condition, string name)
    {
        if (!condition)
        {
            throw new InvalidOperationException($"Assertiva falhou: {name}");
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string name) where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Assertiva falhou em {name}. Esperado: {expected}. Atual: {actual}");
        }
    }

    private sealed class FakeNuGetPackageClient : INuGetPackageClient
    {
        public Task<IReadOnlyList<string>> GetVersionsAsync(string packageId, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Sem conectividade com api.nuget.org");
        }

        public Task<IReadOnlyList<string>> GetAssetFrameworkFoldersAsync(string packageId, string version, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Sem conectividade com api.nuget.org");
        }
    }
}
