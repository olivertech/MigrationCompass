using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Constrói uma visão agregada e não aditiva de exposição econômica orientativa por cenário.
/// </summary>
public static class EconomicExposureBuilder
{
    public static IReadOnlyList<EconomicExposureScenario> Build(SolutionScanResult result, CostEstimator estimator)
    {
        var scenarios = new List<EconomicExposureScenario>();

        AddScenario(
            scenarios,
            "Sustentação operacional",
            "Reflete esforço recorrente de sustentação em componentes legados com alta inércia operacional.",
            CollectRanges(result, estimator, IsOperationalFinding));

        AddScenario(
            scenarios,
            "Perda de produtividade",
            "Representa atrito de desenvolvimento causado por acoplamento estrutural, retrabalho e baixa previsibilidade de mudança.",
            CollectRanges(result, estimator, IsProductivityFinding));

        AddScenario(
            scenarios,
            "Atraso de entregas",
            "Expressa a chance de cronogramas se alongarem em função de bloqueadores distintos, dependências críticas e validações extras.",
            CollectRanges(result, estimator, IsDeliveryFinding));

        AddScenario(
            scenarios,
            "Infraestrutura",
            "Reflete pressão operacional associada a componentes web legados e custos indiretos de manter a base em arquitetura antiga.",
            CollectRanges(result, estimator, IsInfrastructureFinding));

        AddScenario(
            scenarios,
            "Segurança e conformidade",
            "Representa a exposição associada a autenticação, dependências antigas e limitações para adoção de práticas modernas de proteção.",
            CollectRanges(result, estimator, IsSecurityFinding));

        return scenarios;
    }

    private static void AddScenario(List<EconomicExposureScenario> scenarios, string title, string summary, IReadOnlyList<MonthlyCostRange> ranges)
    {
        if (ranges.Count == 0)
        {
            return;
        }

        scenarios.Add(new EconomicExposureScenario
        {
            Title = title,
            Summary = summary,
            Range = AggregateRanges(ranges),
            Signals = ranges.Count
        });
    }

    private static IReadOnlyList<MonthlyCostRange> CollectRanges(SolutionScanResult result, CostEstimator estimator, Func<ApiFinding, bool> apiMatch)
    {
        return result.ApiFindings
            .Where(apiMatch)
            .Select(finding => estimator.Estimate(finding.Rule))
            .Concat(result.PackageFindings
                .Where(finding => apiMatch(ToApiProxy(finding)))
                .Select(estimator.Estimate))
            .ToArray();
    }

    private static ApiFinding ToApiProxy(PackageCompatibilityFinding finding)
    {
        return new ApiFinding
        {
            ProjectName = finding.ProjectName,
            FilePath = string.Empty,
            LineNumber = 0,
            MatchedText = finding.PackageId,
            Rule = new ApiRule
            {
                Id = finding.PackageId,
                Api = finding.PackageId,
                Category = finding.Category,
                Impact = finding.Impact,
                Effort = finding.Effort ?? "Medio",
                Alternative = finding.Recommendation,
                Docs = string.Empty,
                BusinessImpact = finding.BusinessImpact,
                MonthlyInactionCost = finding.EstimatedMonthlyInactionCost,
                EconomicProfile = finding.EconomicProfile
            }
        };
    }

    private static bool IsOperationalFinding(ApiFinding finding)
    {
        return finding.Rule.Api.Contains("System.Web", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("ServiceModel", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("Microsoft.AspNet", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("Owin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProductivityFinding(ApiFinding finding)
    {
        return finding.Rule.Category.Contains("Web", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Category.Contains("Framework", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.BusinessImpact?.Contains("retrabalho", StringComparison.OrdinalIgnoreCase) == true ||
               finding.Rule.BusinessImpact?.Contains("produtividade", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsDeliveryFinding(ApiFinding finding)
    {
        return string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.BusinessImpact?.Contains("cronograma", StringComparison.OrdinalIgnoreCase) == true ||
               finding.Rule.BusinessImpact?.Contains("atras", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static bool IsInfrastructureFinding(ApiFinding finding)
    {
        return finding.Rule.Api.Contains("System.Web", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("WebPages", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("Razor", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("Owin", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSecurityFinding(ApiFinding finding)
    {
        return finding.Rule.Api.Contains("Security", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("Authentication", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("Identity", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Api.Contains("FormsAuthentication", StringComparison.OrdinalIgnoreCase) ||
               finding.Rule.Category.Contains("Security", StringComparison.OrdinalIgnoreCase);
    }

    private static MonthlyCostRange AggregateRanges(IReadOnlyList<MonthlyCostRange> ranges)
    {
        var ordered = ranges
            .OrderByDescending(range => range.Max)
            .ToArray();

        var primary = ordered[0];
        var residualMin = ordered.Skip(1).Sum(range => range.Min) * 0.25m;
        var residualMax = ordered.Skip(1).Sum(range => range.Max) * 0.25m;

        return new MonthlyCostRange
        {
            Min = primary.Min + residualMin,
            Max = primary.Max + residualMax
        };
    }
}
