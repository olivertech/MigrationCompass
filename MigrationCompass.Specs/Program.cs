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
        await RunAsync("Gera parecer gerencial deterministico", TestStrategyAdvisorAsync, failures);
        await RunAsync("Gera HTML esperado", TestHtmlGeneratorAsync, failures);
        await RunAsync("Calcula faixa economica parametrizada", TestCostEstimatorAsync, failures);
        await RunAsync("Detecta sinais heurÃ­sticos de SOLID", TestSolidScannerAsync, failures);
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
        AssertTrue(result.Summary.RiskScore is >= 45 and <= 60, "Score de risco com saturacao gradual");
        AssertTrue(result.Summary.Maintainability.Score > 0, "Score de fragilidade estrutural");
        AssertTrue(result.Summary.Maintainability.MigrationRisk.RawScore >= result.Summary.RiskScore - 1, "Componente risco de migracao");
        return Task.CompletedTask;
    }

    private static Task TestStrategyAdvisorAsync()
    {
        var result = new SolutionScanResult
        {
            SolutionName = "ContabilApp",
            SolutionPath = "ContabilApp.sln",
            Summary = new ReportSummary
            {
                ProjectsScanned = 1,
                CriticalBlockers = 8,
                Warnings = 1,
                InformationalItems = 0,
                RiskScore = 100
            }
        };

        result.Projects.Add(new ProjectScanResult
        {
            ProjectName = "Contabil.Web",
            ProjectPath = "Contabil.Web.csproj",
            TargetFrameworks = ["net481"],
            MigrationProfile = ProjectClassification.Classify(["net481"]),
            PackageReferences = [],
            AssemblyReferences = [],
            SourceFiles = []
        });

        result.ApiFindings.Add(new ApiFinding
        {
            ProjectName = "Contabil.Web",
            FilePath = "Global.asax.cs",
            LineNumber = 10,
            MatchedText = "HttpContext.Current",
            Rule = new ApiRule
            {
                Id = "WEB001",
                Api = "System.Web.HttpContext.Current",
                Category = "WebForms",
                Impact = "Alto",
                Effort = "Medio",
                Alternative = "Migrar",
                Docs = "https://learn.microsoft.com/"
            }
        });

        result.PackageFindings.Add(new PackageCompatibilityFinding
        {
            ProjectName = "Contabil.Web",
            PackageId = "Microsoft.AspNet.Mvc",
            RequestedVersion = "5.2.7",
            Status = "BLOQUEADOR",
            Impact = "Alto",
            Recommendation = "Migrar para ASP.NET Core MVC",
            Details = "Detalhes",
            Effort = "Alto",
            BusinessImpact = "Impacto alto",
            EstimatedMonthlyInactionCost = "R$ 1.000",
            IsBlocker = true,
            IsWarning = false
        });

        result.Advisory = StrategyAdvisor.Build(result);
        AssertTrue(result.Advisory is not null, "Parecer gerado");
        AssertTrue(result.Advisory!.ScenarioNarrative.Contains("reconstru", StringComparison.OrdinalIgnoreCase), "Narrativa do cenÃ¡rio");
        AssertTrue(result.Advisory.RecommendedStrategy.Contains("Reconstru", StringComparison.OrdinalIgnoreCase), "EstratÃ©gia de reconstruÃ§Ã£o");
        AssertTrue(result.Advisory.DecisionDrivers.Count >= 4, "Drivers objetivos");
        AssertTrue(result.Advisory.Paths.Any(path => path.IsRecommended && path.Title.Contains("Reconstru", StringComparison.OrdinalIgnoreCase)), "Caminho recomendado coerente");
        return Task.CompletedTask;
    }

    private static Task TestHtmlGeneratorAsync()
    {
        var result = new SolutionScanResult
        {
            SolutionName = "MinhaSolucao",
            SolutionPath = "C:\\Temp\\MinhaSolucao.sln",
            EconomicParameters = new EconomicParameters
            {
                HourlyRateMin = 100,
                HourlyRateMax = 200,
                WeeksPerMonth = 4,
                Low = new EconomicBand
                {
                    WeeklyHoursMin = 1,
                    WeeklyHoursMax = 2,
                    TeamSizeMin = 1,
                    TeamSizeMax = 1,
                    InfraCostMin = 100,
                    InfraCostMax = 200,
                    RiskMultiplierMin = 1.0m,
                    RiskMultiplierMax = 1.1m
                },
                Medium = new EconomicBand
                {
                    WeeklyHoursMin = 2,
                    WeeklyHoursMax = 4,
                    TeamSizeMin = 1,
                    TeamSizeMax = 2,
                    InfraCostMin = 200,
                    InfraCostMax = 400,
                    RiskMultiplierMin = 1.1m,
                    RiskMultiplierMax = 1.3m
                },
                High = new EconomicBand
                {
                    WeeklyHoursMin = 4,
                    WeeklyHoursMax = 8,
                    TeamSizeMin = 1.5m,
                    TeamSizeMax = 2.5m,
                    InfraCostMin = 500,
                    InfraCostMax = 1200,
                    RiskMultiplierMin = 1.3m,
                    RiskMultiplierMax = 1.6m
                },
                Disclaimer = "Faixas orientativas para assessment inicial."
            },
            Advisory = new SolutionAdvisory
            {
                ExecutiveHeadline = "A soluÃ§Ã£o apresenta distÃ¢ncia tecnolÃ³gica elevada para uma migraÃ§Ã£o direta atÃ© .NET 10.",
                ScenarioNarrative = "A distÃ¢ncia tecnolÃ³gica sugere que a melhor leitura Ã© tratar a evoluÃ§Ã£o como reconstruÃ§Ã£o gradual.",
                RecommendedStrategy = "ReconstruÃ§Ã£o orientada por domÃ­nio, com convivÃªncia gradual entre legado e novos componentes.",
                Rationale = "O volume de bloqueadores e o acoplamento ao legado tornam o salto direto pouco atrativo.",
                ManagerialPositioning = "O caso se aproxima mais de um reposicionamento tecnolÃ³gico do que de uma simples atualizaÃ§Ã£o de versÃ£o.",
                DistanceAssessment = "Toda a amostra permanece em .NET Framework legado.",
                OpportunitySummary = "A organizaÃ§Ã£o deixa de capturar ganhos de observabilidade, seguranÃ§a e simplificaÃ§Ã£o operacional.",
                DecisionDrivers =
                [
                    "1 projeto em .NET Framework 3.x/4.x.",
                    "3 bloqueadores crÃ­ticos relevantes."
                ],
                Paths =
                [
                    new DecisionPathOption
                    {
                        Title = "MigraÃ§Ã£o direta para .NET 10",
                        Fit = "Baixa aderÃªncia ao cenÃ¡rio atual.",
                        Effort = "Alto",
                        IndicativeRisk = "Alto",
                        Guidance = "Usar com cautela.",
                        IsRecommended = false
                    },
                    new DecisionPathOption
                    {
                        Title = "ReconstruÃ§Ã£o gradual com convivÃªncia do legado",
                        Fit = "Boa aderÃªncia ao cenÃ¡rio atual.",
                        Effort = "Alto, porÃ©m mais previsÃ­vel",
                        IndicativeRisk = "MÃ©dio",
                        Guidance = "Fatiar por domÃ­nio.",
                        IsRecommended = true
                    }
                ]
            },
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

        result.EconomicExposureScenarios.Add(new EconomicExposureScenario
        {
            Title = "Sustentação operacional",
            Summary = "Pressão recorrente sobre correções, sustentação e suporte ao legado.",
            Signals = 3,
            Range = new MonthlyCostRange { Min = 1200, Max = 2800 }
        });

        result.GeneratedArtifacts.Add(new GeneratedArtifactInfo
        {
            ProjectName = "Legacy.Web",
            FilePath = "Legacy.generated.cs",
            Category = "Código gerado",
            Reason = "Arquivo com sufixo .generated.cs"
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

        result.SolidFindings.Add(new SolidFinding
        {
            ProjectName = "Legacy.Web",
            FilePath = "Legacy.cs",
            Principle = "SRP",
            Severity = "Alto",
            Confidence = "Baixa",
            TargetName = "LegacyManager",
            Evidence = "Classe com muitas linhas.",
            Explanation = "HÃ¡ indÃ­cio de mÃºltiplas responsabilidades concentradas.",
            Recommendation = "Dividir por responsabilidade.",
            LineNumber = 10
        });

        result.SolidFindings.Add(new SolidFinding
        {
            ProjectName = "Legacy.Web",
            FilePath = "Legacy.cs",
            Principle = "DIP",
            Severity = "Alto",
            Confidence = "Baixa",
            TargetName = "LegacyManager",
            Evidence = "Classe instancia dependencias concretas diretamente.",
            Explanation = "Ha indicio de alto acoplamento a implementacoes concretas.",
            Recommendation = "Inverter dependencias.",
            LineNumber = 10
        });

        result.SolidFindings.Add(new SolidFinding
        {
            ProjectName = "Legacy.Web",
            FilePath = "Legacy.cs",
            Principle = "OCP",
            Severity = "Alto",
            Confidence = "Baixa",
            TargetName = "LegacyManager",
            Evidence = "Fluxo com decisoes extensas por condicao.",
            Explanation = "Ha indicio de baixa extensibilidade sem alterar codigo existente.",
            Recommendation = "Extrair estrategias.",
            LineNumber = 10
        });

        result.SolidFindings.Add(new SolidFinding
        {
            ProjectName = "Legacy.Web",
            FilePath = "Legacy.cs",
            Principle = "LSP",
            Severity = "Medio",
            Confidence = "Baixa",
            TargetName = "LegacyManager",
            Evidence = "Substituicoes potencialmente frageis.",
            Explanation = "Ha indicio de herancas com comportamento inconsistente.",
            Recommendation = "Revisar hierarquia.",
            LineNumber = 10
        });

        var html = new HtmlReportGenerator().Generate(result);
        AssertTrue(html.Contains("Relatório Executivo de Migração para .NET 10", StringComparison.Ordinal), "Cabecalho");
        AssertTrue(html.Contains("Pontuação de risco: 100/100", StringComparison.Ordinal), "Score");
        AssertTrue(html.Contains("Índice de fragilidade estrutural", StringComparison.Ordinal), "Score de fragilidade");
        AssertTrue(html.Contains("Índice de Fragilidade Estrutural", StringComparison.Ordinal), "Secao fragilidade");
        AssertTrue(html.Contains("Legenda gerencial da fragilidade estrutural", StringComparison.Ordinal), "Legenda gerencial");
        AssertTrue(html.Contains("85 a 100 - Crítica", StringComparison.Ordinal), "Faixa critica");
        AssertTrue(html.Contains("Cenário com Maior Aderência aos Sinais Encontrados", StringComparison.Ordinal), "Cenario consultivo");
        AssertTrue(html.Contains("Bloqueadores Críticos Relevantes", StringComparison.Ordinal), "Titulo executivo");
        AssertTrue(html.Contains("Leitura Gerencial", StringComparison.Ordinal), "Leitura gerencial");
        AssertTrue(html.Contains("Caminhos Estratégicos Possíveis", StringComparison.Ordinal), "Caminhos estrategicos");
        AssertTrue(html.Contains("Base técnica da leitura", StringComparison.Ordinal), "Base tecnica consultiva");
        AssertTrue(html.Contains("Cenário com maior aderência aos sinais encontrados", StringComparison.Ordinal), "Posicionamento consultivo");
        AssertTrue(html.Contains("HttpContext.Current aumenta acoplamento", StringComparison.Ordinal), "Impacto de negocio");
        AssertTrue(html.Contains("Sinais estruturais que merecem revisão", StringComparison.Ordinal), "Secao solid");
        AssertTrue(html.Contains("Legenda Executiva dos Princípios SOLID", StringComparison.Ordinal), "Legenda solid");
        AssertTrue(html.Contains("Single Responsibility Principle", StringComparison.Ordinal), "Definicao SRP");
        AssertTrue(html.Contains("Dependency Inversion Principle", StringComparison.Ordinal), "Definicao DIP");
        AssertTrue(html.Contains("Resumo por Princípio", StringComparison.Ordinal), "Resumo por principio");
        AssertTrue(html.Contains("SRP, OCP, LSP, DIP", StringComparison.Ordinal), "Agregacao de principios por alvo");
        AssertTrue(html.Contains("solid-badge-multi-4", StringComparison.Ordinal), "Badge multiplo principio");
        AssertTrue(html.Contains("solid-multi-4-row", StringComparison.Ordinal), "Classe visual nivel 4");
        AssertTrue(html.Contains("solid-multi-4-cell", StringComparison.Ordinal), "Borda visual nivel 4");
        AssertTrue(html.Contains("Exposição Econômica Orientativa por Cenário", StringComparison.Ordinal), "Secao economica agregada");
        AssertTrue(html.Contains("Artefatos gerados ou de scaffolding identificados", StringComparison.Ordinal), "Secao artefatos");
        AssertTrue(!html.Contains("Custo Estimado de Inação (Mensal)", StringComparison.Ordinal), "Remove custo individual por bloqueador");
        AssertTrue(html.Contains("Premissas Econômicas", StringComparison.Ordinal), "Premissas");
        AssertTrue(html.Contains("Faixas orientativas para assessment inicial.", StringComparison.Ordinal), "Disclaimer");
        AssertTrue(html.Contains("discovery técnico e de negócio", StringComparison.Ordinal), "Disclaimer executivo");
        return Task.CompletedTask;
    }

    private static Task TestCostEstimatorAsync()
    {
        var parameters = new EconomicParameters
        {
            HourlyRateMin = 100,
            HourlyRateMax = 200,
            WeeksPerMonth = 4,
            Low = new EconomicBand
            {
                WeeklyHoursMin = 1,
                WeeklyHoursMax = 2,
                TeamSizeMin = 1,
                TeamSizeMax = 1,
                InfraCostMin = 100,
                InfraCostMax = 200,
                RiskMultiplierMin = 1.0m,
                RiskMultiplierMax = 1.1m
            },
            Medium = new EconomicBand
            {
                WeeklyHoursMin = 2,
                WeeklyHoursMax = 4,
                TeamSizeMin = 1,
                TeamSizeMax = 2,
                InfraCostMin = 200,
                InfraCostMax = 400,
                RiskMultiplierMin = 1.1m,
                RiskMultiplierMax = 1.3m
            },
            High = new EconomicBand
            {
                WeeklyHoursMin = 4,
                WeeklyHoursMax = 8,
                TeamSizeMin = 1.5m,
                TeamSizeMax = 2.5m,
                InfraCostMin = 500,
                InfraCostMax = 1200,
                RiskMultiplierMin = 1.3m,
                RiskMultiplierMax = 1.6m
            },
            Disclaimer = "Teste"
        };

        var estimator = new CostEstimator(parameters);
        var range = estimator.Estimate(new ApiRule
        {
            Id = "RULE001",
            Api = "Legacy.Api",
            Category = "Legacy",
            Impact = "Alto",
            Effort = "Alto",
            Alternative = "Migrar",
            Docs = "https://example.org"
        });

        AssertTrue(range.Min > 0, "Faixa minima positiva");
        AssertTrue(range.Max > range.Min, "Faixa maxima maior");
        AssertTrue(CostEstimator.Format(range).Contains("R$", StringComparison.Ordinal), "Formatacao monetaria");
        return Task.CompletedTask;
    }

    private static async Task TestSolidScannerAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"migrationcompass-solid-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var filePath = Path.Combine(tempRoot, "LegacyManager.cs");

        try
        {
            var source = """
using System;

public interface ILegacyService
{
    void A();
    void B();
    void C();
    void D();
    void E();
    void F();
    void G();
    void H();
    void I();
}

public class LegacyManager
{
    private readonly object _a;
    private readonly object _b;
    private readonly object _c;
    private readonly object _d;
    private readonly object _e;
    private readonly object _f;

    public LegacyManager(object a, object b, object c, object d, object e, object f, object g)
    {
        _a = a;
        _b = b;
        _c = c;
        _d = d;
        _e = e;
        _f = f;
    }

    public void Process(int kind)
    {
        var x1 = new object();
        var x2 = new object();
        var x3 = new object();
        var x4 = new object();
        switch (kind)
        {
            case 1: Console.WriteLine("a"); break;
            case 2: Console.WriteLine("b"); break;
            default: Console.WriteLine("c"); break;
        }

        if (kind == 1) { }
        else if (kind == 2) { }
        else if (kind == 3) { }
        else if (kind == 4) { }
        else if (kind == 5) { }
    }
}

public class DirectInstantiationOnly
{
    public void Build()
    {
        var a = new object();
        var b = new object();
        var c = new object();
        var d = new object();
        var e = new object();
        var f = new object();
    }
}
""";

            await File.WriteAllTextAsync(filePath, source);

            var project = new ProjectScanResult
            {
                ProjectName = "Legacy.Project",
                ProjectPath = "Legacy.Project.csproj",
                TargetFrameworks = ["net481"],
                MigrationProfile = ProjectClassification.Classify(["net481"]),
                PackageReferences = [],
                AssemblyReferences = [],
                SourceFiles = [filePath]
            };

            var findings = await new SolidScanner().ScanAsync([project], CancellationToken.None);
            AssertTrue(findings.Any(finding => finding.Principle == "ISP"), "ISP detectado");
            AssertTrue(findings.Any(finding => finding.Principle == "DIP"), "DIP detectado");
            AssertTrue(findings.Any(finding => finding.Principle == "OCP"), "OCP detectado");
            var directInstantiationFinding = findings.Single(finding => finding.Principle == "DIP" && finding.TargetName == "DirectInstantiationOnly");
            AssertTrue(!directInstantiationFinding.Evidence.Contains("0 parâmetro(s)", StringComparison.Ordinal), "Nao exibe construtor zerado");
            AssertTrue(!directInstantiationFinding.Evidence.Contains("0 campo(s) de dependência", StringComparison.Ordinal), "Nao exibe dependencia zerada");
            AssertTrue(directInstantiationFinding.Evidence.Contains("6 instanciação(ões) direta(s)", StringComparison.Ordinal), "Mantem evidencia relevante");
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
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
                BusinessImpact = "AutoMapper exige revisÃ£o de profiles.",
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
                BusinessImpact = "Serilog demanda apenas alinhamento de configuraÃ§Ã£o.",
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
        AssertTrue(findings.Any(finding => finding.PackageId == "AutoMapper.Extensions.Microsoft.DependencyInjection" && finding.BusinessImpact == "AutoMapper exige revisÃ£o de profiles."), "AutoMapper reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "Serilog.AspNetCore" && finding.BusinessImpact == "Serilog demanda apenas alinhamento de configuraÃ§Ã£o."), "Serilog reconhecido");
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
                BusinessImpact = "MassTransit exige validaÃ§Ã£o de contratos e filas.",
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
                BusinessImpact = "RabbitMQ.Client exige testes de conexÃ£o e reconexÃ£o.",
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
                Alternative = "Revisar integraÃ§Ã£o com DI",
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
                BusinessImpact = "MediatR Ã© mais ajuste de composiÃ§Ã£o do que bloqueio estrutural.",
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
                BusinessImpact = "Castle.Core pode exigir revisÃ£o de interceptaÃ§Ã£o e AOP.",
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
        AssertTrue(findings.Any(finding => finding.PackageId == "MassTransit.RabbitMq" && finding.BusinessImpact == "MassTransit exige validaÃ§Ã£o de contratos e filas."), "MassTransit reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "RabbitMQ.Client" && finding.BusinessImpact == "RabbitMQ.Client exige testes de conexÃ£o e reconexÃ£o."), "RabbitMQ.Client reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "FluentValidation.AspNetCore" && finding.BusinessImpact == "FluentValidation exige apenas ajustes de bootstrap."), "FluentValidation reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "MediatR.Extensions.Microsoft.DependencyInjection" && finding.BusinessImpact == "MediatR Ã© mais ajuste de composiÃ§Ã£o do que bloqueio estrutural."), "MediatR reconhecido");
        AssertTrue(findings.Any(finding => finding.PackageId == "Castle.Core" && finding.BusinessImpact == "Castle.Core pode exigir revisÃ£o de interceptaÃ§Ã£o e AOP."), "Castle.Core reconhecido");
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

