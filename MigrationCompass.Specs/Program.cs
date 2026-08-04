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
        await RunAsync("Ignora pacotes irrelevantes", TestIrrelevantPackagesAsync, failures);
        await RunAsync("Aplica insight de negocio para pacote bloqueador", TestPackageBusinessInsightAsync, failures);
        await RunAsync("Reconhece familias de pacotes com curinga", TestWildcardPackageRulesAsync, failures);
        await RunAsync("Reconhece NHibernate AutoMapper e Serilog", TestExpandedLegacyPackagesAsync, failures);
        await RunAsync("Reconhece mensageria validacao e infraestrutura", TestBroaderServerSideCatalogAsync, failures);

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
                BusinessImpact = "Impacto alto",
                MonthlyInactionCost = "R$ 1.000",
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
            Effort = "Medio",
            BusinessImpact = "Ajuste simples",
            EstimatedMonthlyInactionCost = "R$ 500",
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
                BusinessImpact = "HttpContext.Current aumenta acoplamento e custo operacional.",
                MonthlyInactionCost = "R$ 1.000",
                Docs = "https://learn.microsoft.com/"
            }
        });

        var html = new HtmlReportGenerator().Generate(result);
        AssertTrue(html.Contains("Relatorio Executivo de Migracao para .NET 10", StringComparison.Ordinal), "Cabecalho");
        AssertTrue(html.Contains("Pontuacao de risco: 100/100", StringComparison.Ordinal), "Score");
        AssertTrue(html.Contains("🚨 Bloqueadores Criticos", StringComparison.Ordinal), "Titulo executivo");
        AssertTrue(html.Contains("HttpContext.Current aumenta acoplamento", StringComparison.Ordinal), "Impacto de negocio");
        return Task.CompletedTask;
    }

    private static async Task TestNuGetOfflineFallbackAsync()
    {
        var checker = new NuGetChecker(new FakeNuGetPackageClient(), []);
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

    private static async Task TestIrrelevantPackagesAsync()
    {
        var checker = new NuGetChecker(
            new FakeNuGetPackageClient(),
            [],
            new HashSet<string>(["jquery", "bootstrap"], StringComparer.OrdinalIgnoreCase));

        var project = new ProjectScanResult
        {
            ProjectName = "Legacy.Web",
            ProjectPath = "Legacy.Web.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences =
            [
                new PackageReferenceInfo { PackageId = "jquery", Version = "3.7.1", PrivateAssets = null, IsFromPackagesConfig = true },
                new PackageReferenceInfo { PackageId = "Microsoft.AspNet.Mvc", Version = "5.2.7", PrivateAssets = null, IsFromPackagesConfig = true }
            ],
            AssemblyReferences = [],
            SourceFiles = []
        };

        var findings = await checker.CheckAsync([project], CancellationToken.None);
        AssertEqual(1, findings.Count, "Pacote irrelevante ignorado");
        AssertEqual("Microsoft.AspNet.Mvc", findings.Single().PackageId, "Pacote server-side mantido");
    }

    private static async Task TestPackageBusinessInsightAsync()
    {
        var rules = new[]
        {
            new ApiRule
            {
                Id = "NUGET001",
                AppliesTo = "package",
                PackageId = "Microsoft.AspNet.Mvc",
                Api = "Microsoft.AspNet.Mvc",
                Category = "Web Framework",
                Impact = "Alto",
                Effort = "Alto",
                Alternative = "Migrar para ASP.NET Core MVC",
                BusinessImpact = "Impede middleware moderno e amplia custo operacional.",
                MonthlyInactionCost = "R$ 3.200",
                Docs = "https://learn.microsoft.com/"
            }
        };

        var checker = new NuGetChecker(new IncompatibleNuGetPackageClient(), rules);
        var project = new ProjectScanResult
        {
            ProjectName = "Contabil.Web",
            ProjectPath = "Contabil.Web.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences = [new PackageReferenceInfo { PackageId = "Microsoft.AspNet.Mvc", Version = "5.2.7", PrivateAssets = null, IsFromPackagesConfig = true }],
            AssemblyReferences = [],
            SourceFiles = []
        };

        var findings = await checker.CheckAsync([project], CancellationToken.None);
        var finding = findings.Single();
        AssertTrue(finding.IsBlocker, "Pacote classificado como bloqueador");
        AssertEqual("Impede middleware moderno e amplia custo operacional.", finding.BusinessImpact!, "Insight de negocio aplicado");
        AssertEqual("R$ 3.200", finding.EstimatedMonthlyInactionCost!, "Custo mensal aplicado");
    }

    private static async Task TestWildcardPackageRulesAsync()
    {
        var rules = new[]
        {
            new ApiRule
            {
                Id = "NUGET006",
                AppliesTo = "package",
                PackageId = "Microsoft.Owin.*",
                Api = "Microsoft.Owin.*",
                Category = "Pipeline HTTP",
                Impact = "Alto",
                Effort = "Alto",
                Alternative = "Migrar middlewares OWIN",
                BusinessImpact = "Pipeline OWIN legado exige redesenho do bootstrap.",
                MonthlyInactionCost = "R$ 5.000",
                Docs = "https://learn.microsoft.com/"
            }
        };

        var checker = new NuGetChecker(new IncompatibleNuGetPackageClient(), rules);
        var project = new ProjectScanResult
        {
            ProjectName = "Portal.Web",
            ProjectPath = "Portal.Web.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences = [new PackageReferenceInfo { PackageId = "Microsoft.Owin.Security.Cookies", Version = "4.2.2", PrivateAssets = null, IsFromPackagesConfig = true }],
            AssemblyReferences = [],
            SourceFiles = []
        };

        var findings = await checker.CheckAsync([project], CancellationToken.None);
        var finding = findings.Single();
        AssertTrue(finding.IsBlocker, "Wildcard reconhecido como bloqueador");
        AssertEqual("Pipeline OWIN legado exige redesenho do bootstrap.", finding.BusinessImpact!, "Insight via curinga");
        AssertEqual("R$ 5.000", finding.EstimatedMonthlyInactionCost!, "Custo via curinga");
    }

    private static async Task TestExpandedLegacyPackagesAsync()
    {
        var rules = new[]
        {
            new ApiRule
            {
                Id = "NUGET026",
                AppliesTo = "package",
                PackageId = "NHibernate*",
                Api = "NHibernate*",
                Category = "Acesso a Dados",
                Impact = "Alto",
                Effort = "Alto",
                Alternative = "Revisar camada ORM",
                BusinessImpact = "NHibernate aumenta discovery e risco de regressao.",
                MonthlyInactionCost = "R$ 8.000",
                Docs = "https://nhibernate.info/"
            },
            new ApiRule
            {
                Id = "NUGET027",
                AppliesTo = "package",
                PackageId = "AutoMapper*",
                Api = "AutoMapper*",
                Category = "Mapeamento de Objetos",
                Impact = "Medio",
                Effort = "Medio",
                Alternative = "Revisar profiles",
                BusinessImpact = "AutoMapper exige revisão de profiles.",
                MonthlyInactionCost = "R$ 2.000",
                Docs = "https://docs.automapper.org/"
            },
            new ApiRule
            {
                Id = "NUGET028",
                AppliesTo = "package",
                PackageId = "Serilog*",
                Api = "Serilog*",
                Category = "Logging",
                Impact = "Baixo",
                Effort = "Baixo",
                Alternative = "Validar sinks e hosting",
                BusinessImpact = "Serilog demanda apenas alinhamento de configuração.",
                MonthlyInactionCost = "R$ 800",
                Docs = "https://serilog.net/"
            }
        };

        var checker = new NuGetChecker(new IncompatibleNuGetPackageClient(), rules);
        var project = new ProjectScanResult
        {
            ProjectName = "Legacy.Backend",
            ProjectPath = "Legacy.Backend.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences =
            [
                new PackageReferenceInfo { PackageId = "NHibernate", Version = "5.4.0", PrivateAssets = null, IsFromPackagesConfig = true },
                new PackageReferenceInfo { PackageId = "AutoMapper.Extensions.Microsoft.DependencyInjection", Version = "12.0.1", PrivateAssets = null, IsFromPackagesConfig = false },
                new PackageReferenceInfo { PackageId = "Serilog.AspNetCore", Version = "8.0.0", PrivateAssets = null, IsFromPackagesConfig = false }
            ],
            AssemblyReferences = [],
            SourceFiles = []
        };

        var findings = await checker.CheckAsync([project], CancellationToken.None);
        AssertEqual(3, findings.Count, "Novos pacotes catalogados");
        AssertTrue(findings.Any(finding => finding.PackageId == "NHibernate" && finding.BusinessImpact == "NHibernate aumenta discovery e risco de regressao."), "NHibernate reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "AutoMapper.Extensions.Microsoft.DependencyInjection" && finding.BusinessImpact == "AutoMapper exige revisão de profiles."), "AutoMapper reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "Serilog.AspNetCore" && finding.BusinessImpact == "Serilog demanda apenas alinhamento de configuração."), "Serilog reconhecido");
    }

    private static async Task TestBroaderServerSideCatalogAsync()
    {
        var rules = new[]
        {
            new ApiRule
            {
                Id = "NUGET029",
                AppliesTo = "package",
                PackageId = "MassTransit*",
                Api = "MassTransit*",
                Category = "Mensageria",
                Impact = "Medio",
                Effort = "Medio",
                Alternative = "Revisar buses e consumers",
                BusinessImpact = "MassTransit exige validação de contratos e filas.",
                MonthlyInactionCost = "R$ 4.000",
                Docs = "https://masstransit.io/"
            },
            new ApiRule
            {
                Id = "NUGET030",
                AppliesTo = "package",
                PackageId = "RabbitMQ.Client",
                Api = "RabbitMQ.Client",
                Category = "Mensageria",
                Impact = "Medio",
                Effort = "Medio",
                Alternative = "Revisar client e TLS",
                BusinessImpact = "RabbitMQ.Client exige testes de conexão e reconexão.",
                MonthlyInactionCost = "R$ 3.000",
                Docs = "https://rabbitmq.com/"
            },
            new ApiRule
            {
                Id = "NUGET031",
                AppliesTo = "package",
                PackageId = "FluentValidation*",
                Api = "FluentValidation*",
                Category = "Validacao",
                Impact = "Baixo",
                Effort = "Baixo",
                Alternative = "Revisar integração com DI",
                BusinessImpact = "FluentValidation exige apenas ajustes de bootstrap.",
                MonthlyInactionCost = "R$ 900",
                Docs = "https://fluentvalidation.net/"
            },
            new ApiRule
            {
                Id = "NUGET032",
                AppliesTo = "package",
                PackageId = "MediatR*",
                Api = "MediatR*",
                Category = "Orquestracao Interna",
                Impact = "Baixo",
                Effort = "Baixo",
                Alternative = "Revisar handlers e behaviors",
                BusinessImpact = "MediatR é mais ajuste de composição do que bloqueio estrutural.",
                MonthlyInactionCost = "R$ 700",
                Docs = "https://github.com/jbogard/MediatR"
            },
            new ApiRule
            {
                Id = "NUGET033",
                AppliesTo = "package",
                PackageId = "Castle.Core",
                Api = "Castle.Core",
                Category = "Infraestrutura",
                Impact = "Medio",
                Effort = "Medio",
                Alternative = "Mapear interceptors e proxies",
                BusinessImpact = "Castle.Core pode exigir revisão de interceptação e AOP.",
                MonthlyInactionCost = "R$ 2.500",
                Docs = "https://castleproject.org/"
            }
        };

        var checker = new NuGetChecker(new IncompatibleNuGetPackageClient(), rules);
        var project = new ProjectScanResult
        {
            ProjectName = "Legacy.Integrations",
            ProjectPath = "Legacy.Integrations.csproj",
            TargetFrameworks = ["net48"],
            MigrationProfile = ProjectClassification.Classify(["net48"]),
            PackageReferences =
            [
                new PackageReferenceInfo { PackageId = "MassTransit.RabbitMq", Version = "8.0.0", PrivateAssets = null, IsFromPackagesConfig = false },
                new PackageReferenceInfo { PackageId = "RabbitMQ.Client", Version = "6.8.1", PrivateAssets = null, IsFromPackagesConfig = false },
                new PackageReferenceInfo { PackageId = "FluentValidation.AspNetCore", Version = "11.3.0", PrivateAssets = null, IsFromPackagesConfig = false },
                new PackageReferenceInfo { PackageId = "MediatR.Extensions.Microsoft.DependencyInjection", Version = "11.1.0", PrivateAssets = null, IsFromPackagesConfig = false },
                new PackageReferenceInfo { PackageId = "Castle.Core", Version = "5.1.1", PrivateAssets = null, IsFromPackagesConfig = false }
            ],
            AssemblyReferences = [],
            SourceFiles = []
        };

        var findings = await checker.CheckAsync([project], CancellationToken.None);
        AssertEqual(5, findings.Count, "Pacotes ampliados reconhecidos");
        AssertTrue(findings.Any(finding => finding.PackageId == "MassTransit.RabbitMq" && finding.BusinessImpact == "MassTransit exige validação de contratos e filas."), "MassTransit reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "RabbitMQ.Client" && finding.BusinessImpact == "RabbitMQ.Client exige testes de conexão e reconexão."), "RabbitMQ.Client reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "FluentValidation.AspNetCore" && finding.BusinessImpact == "FluentValidation exige apenas ajustes de bootstrap."), "FluentValidation reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "MediatR.Extensions.Microsoft.DependencyInjection" && finding.BusinessImpact == "MediatR é mais ajuste de composição do que bloqueio estrutural."), "MediatR reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "Castle.Core" && finding.BusinessImpact == "Castle.Core pode exigir revisão de interceptação e AOP."), "Castle.Core reconhecido");
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

    private sealed class IncompatibleNuGetPackageClient : INuGetPackageClient
    {
        public Task<IReadOnlyList<string>> GetVersionsAsync(string packageId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>(["5.2.7"]);
        }

        public Task<IReadOnlyList<string>> GetAssetFrameworkFoldersAsync(string packageId, string version, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<string>>(["net45"]);
        }
    }
}
