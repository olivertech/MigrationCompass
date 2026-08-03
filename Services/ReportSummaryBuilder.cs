using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Converte os achados tÃ©cnicos em mÃ©tricas executivas consumidas pelo relatÃ³rio.
/// </summary>
public static class ReportSummaryBuilder
{
    /// <summary>
    /// Calcula totais e a pontuaÃ§Ã£o de risco com base nas regras atuais do produto.
    /// </summary>
    public static ReportSummary Build(SolutionScanResult result)
    {
        var apiBlockers = result.ApiFindings.Count(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase));
        var apiWarnings = result.ApiFindings.Count(finding => string.Equals(finding.Rule.Impact, "Medio", StringComparison.OrdinalIgnoreCase) || string.Equals(finding.Rule.Impact, "Baixo", StringComparison.OrdinalIgnoreCase));
        var packageBlockers = result.PackageFindings.Count(finding => finding.IsBlocker);
        var packageWarnings = result.PackageFindings.Count(finding => finding.IsWarning);

        var blockers = apiBlockers + packageBlockers;
        var warnings = apiWarnings + packageWarnings;
        var info = result.PackageFindings.Count - packageBlockers - packageWarnings;
        var totalProjects = Math.Max(result.Projects.Count, 1);
        var rawScore = ((blockers * 12.0) + (warnings * 6.0)) / totalProjects * 10.0;
        var riskScore = Math.Min(100, (int)Math.Round(rawScore, MidpointRounding.AwayFromZero));

        return new ReportSummary
        {
            ProjectsScanned = result.Projects.Count,
            CriticalBlockers = blockers,
            Warnings = warnings,
            InformationalItems = info,
            RiskScore = riskScore
        };
    }
}