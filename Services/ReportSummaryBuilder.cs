using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Converte os achados técnicos em métricas executivas consumidas pelo relatório.
/// </summary>
public static class ReportSummaryBuilder
{
    /// <summary>
    /// Calcula totais, pontuação de risco e índice de fragilidade estrutural.
    /// </summary>
    public static ReportSummary Build(SolutionScanResult result)
    {
        var apiBlockers = result.ApiFindings.Count(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase));
        var apiWarnings = result.ApiFindings.Count(finding => IsWarningImpact(finding.Rule.Impact));
        var packageBlockers = result.PackageFindings.Count(finding => finding.IsBlocker);
        var packageWarnings = result.PackageFindings.Count(finding => finding.IsWarning);

        var blockers = apiBlockers + packageBlockers;
        var warnings = apiWarnings + packageWarnings;
        var info = result.PackageFindings.Count - packageBlockers - packageWarnings;
        var riskScore = CalculateRiskScore(result);

        return new ReportSummary
        {
            ProjectsScanned = result.Projects.Count,
            CriticalBlockers = blockers,
            Warnings = warnings,
            InformationalItems = info,
            RiskScore = riskScore,
            Maintainability = BuildMaintainability(result, riskScore)
        };
    }

    private static MaintainabilityAssessment BuildMaintainability(SolutionScanResult result, int riskScore)
    {
        const int migrationRiskWeight = 35;
        const int solidDensityWeight = 25;
        const int technologicalAgeWeight = 20;
        const int legacyCouplingWeight = 20;

        var totalProjects = Math.Max(result.Projects.Count, 1);

        var migrationRiskRaw = riskScore;

        var solidSeverityPoints = result.SolidFindings.Sum(finding => finding.Severity.Trim().ToLowerInvariant() switch
        {
            "alto" => 3,
            "médio" => 2,
            "medio" => 2,
            _ => 1
        });
        var solidDensityPerProject = (double)solidSeverityPoints / totalProjects;
        var solidDensityRaw = ClampScore(solidDensityPerProject * 18.0);

        var averageAgeWeight = result.Projects.Count == 0
            ? 1.0
            : result.Projects.Average(project => project.MigrationProfile.Weight);
        var technologicalAgeRaw = ClampScore(((averageAgeWeight - 1.0) / 3.0) * 100.0);

        var legacyPackageSignals = result.PackageFindings.Count(finding =>
            finding.IsBlocker ||
            finding.PackageId.StartsWith("Microsoft.AspNet.", StringComparison.OrdinalIgnoreCase) ||
            finding.PackageId.StartsWith("Microsoft.Owin", StringComparison.OrdinalIgnoreCase) ||
            finding.PackageId.Equals("Owin", StringComparison.OrdinalIgnoreCase) ||
            finding.PackageId.StartsWith("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
            finding.PackageId.StartsWith("NHibernate", StringComparison.OrdinalIgnoreCase));
        var legacyApiSignals = result.ApiFindings.Count(finding =>
            finding.Rule.Api.StartsWith("System.Web", StringComparison.OrdinalIgnoreCase) ||
            finding.Rule.Api.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase) ||
            finding.Rule.Api.StartsWith("FormsAuthentication", StringComparison.OrdinalIgnoreCase));
        var couplingSignalPerProject = (double)(legacyPackageSignals + legacyApiSignals) / totalProjects;
        var legacyCouplingRaw = ClampScore(couplingSignalPerProject * 22.0);

        var migrationRiskWeighted = Weighted(migrationRiskRaw, migrationRiskWeight);
        var solidDensityWeighted = Weighted(solidDensityRaw, solidDensityWeight);
        var technologicalAgeWeighted = Weighted(technologicalAgeRaw, technologicalAgeWeight);
        var legacyCouplingWeighted = Weighted(legacyCouplingRaw, legacyCouplingWeight);

        var finalScore = Math.Min(100, migrationRiskWeighted + solidDensityWeighted + technologicalAgeWeighted + legacyCouplingWeighted);
        var classification = finalScore switch
        {
            >= 85 => "Crítica",
            >= 65 => "Alta",
            >= 40 => "Moderada",
            _ => "Controlável"
        };

        var summary = classification switch
        {
            "Crítica" => "A solution combina legado tecnológico, forte acoplamento e sinais estruturais que elevam de forma importante a fragilidade da base para manter, adaptar e evoluir.",
            "Alta" => "A base atual apresenta fragilidades estruturais relevantes e deve ser tratada com governança, priorização e redução progressiva de complexidade.",
            "Moderada" => "Há fragilidades relevantes, mas o cenário ainda permite modernização progressiva com risco controlado quando bem priorizada.",
            _ => "A fragilidade estrutural observada é relativamente mais controlável, embora a base ainda exija disciplina técnica para sustentar evolução."
        };

        return new MaintainabilityAssessment
        {
            Score = finalScore,
            Classification = classification,
            ExecutiveSummary = summary,
            MigrationRisk = new MaintainabilityComponent
            {
                Name = "Risco de migração",
                RawScore = migrationRiskRaw,
                WeightedScore = migrationRiskWeighted,
                WeightPercent = migrationRiskWeight,
                Explanation = $"Reflete a pressão acumulada de bloqueadores distintos, avisos recorrentes e diversidade de frentes críticas no caminho até .NET 10. Score bruto atual: {migrationRiskRaw}/100."
            },
            SolidDensity = new MaintainabilityComponent
            {
                Name = "Densidade de sinais SOLID",
                RawScore = solidDensityRaw,
                WeightedScore = solidDensityWeighted,
                WeightPercent = solidDensityWeight,
                Explanation = $"Combina o volume e a severidade de indícios heurísticos de SRP, OCP, LSP, ISP e DIP. Pontos ponderados por projeto: {solidDensityPerProject:0.0}."
            },
            TechnologicalAge = new MaintainabilityComponent
            {
                Name = "Idade tecnológica",
                RawScore = technologicalAgeRaw,
                WeightedScore = technologicalAgeWeighted,
                WeightPercent = technologicalAgeWeight,
                Explanation = $"Usa a distância média dos TFMs em relação ao alvo .NET 10. Peso médio observado dos projetos: {averageAgeWeight:0.0}."
            },
            LegacyCoupling = new MaintainabilityComponent
            {
                Name = "Acoplamento a legado",
                RawScore = legacyCouplingRaw,
                WeightedScore = legacyCouplingWeighted,
                WeightPercent = legacyCouplingWeight,
                Explanation = $"Mede a incidência de dependências e APIs fortemente associadas ao legado web clássico e a componentes com alta inércia estrutural. Sinais por projeto: {couplingSignalPerProject:0.0}."
            }
        };
    }

    private static int CalculateRiskScore(SolutionScanResult result)
    {
        var criticalUnits = result.ApiFindings
            .Where(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase))
            .Select(finding => $"{finding.ProjectName}|API|{finding.Rule.Id}")
            .Concat(result.PackageFindings
                .Where(finding => finding.IsBlocker)
                .Select(finding => $"{finding.ProjectName}|PKG|{finding.PackageId}"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var warningUnits = result.ApiFindings
            .Where(finding => IsWarningImpact(finding.Rule.Impact))
            .Select(finding => $"{finding.ProjectName}|API|{finding.Rule.Id}")
            .Concat(result.PackageFindings
                .Where(finding => finding.IsWarning)
                .Select(finding => $"{finding.ProjectName}|PKG|{finding.PackageId}"))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var diversityUnits = result.ApiFindings
            .Where(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase))
            .Select(finding => finding.Rule.Category)
            .Concat(result.PackageFindings
                .Where(finding => finding.IsBlocker)
                .Select(finding => finding.Category))
            .Where(category => !string.IsNullOrWhiteSpace(category))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var projectFactor = Math.Max(1, result.Projects.Count);
        var totalCodeLines = CountTotalCodeLines(result);
        var klocFactor = Math.Max(1.0, totalCodeLines / 1000.0);
        var pressure = ((criticalUnits * 18.0) + (warningUnits * 7.0) + (diversityUnits * 10.0)) /
                       ((0.65 * projectFactor) + (0.35 * Math.Sqrt(klocFactor)));

        var riskScore = 100.0 * (1.0 - Math.Exp(-pressure / 30.0));
        return ClampScore(riskScore);
    }

    private static int CountTotalCodeLines(SolutionScanResult result)
    {
        var total = 0;
        foreach (var filePath in result.Projects.SelectMany(project => project.SourceFiles).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                total += File.ReadLines(filePath).Count();
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return total;
    }

    private static bool IsWarningImpact(string impact)
    {
        return string.Equals(impact, "Medio", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(impact, "Médio", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(impact, "Baixo", StringComparison.OrdinalIgnoreCase);
    }

    private static int Weighted(int rawScore, int weightPercent)
    {
        return (int)Math.Round(rawScore * (weightPercent / 100.0), MidpointRounding.AwayFromZero);
    }

    private static int ClampScore(double score)
    {
        return Math.Min(100, Math.Max(0, (int)Math.Round(score, MidpointRounding.AwayFromZero)));
    }
}
