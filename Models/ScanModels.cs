namespace MigrationCompass.Models;

/// <summary>
/// Representa os parÃ¢metros mÃ­nimos necessÃ¡rios para iniciar uma execuÃ§Ã£o de scan.
/// </summary>
public sealed record ScanRequest(string SolutionPath, string OutputDirectory, string ReportFormat);

/// <summary>
/// Consolida o resultado completo do scan de uma solution.
/// </summary>
public sealed class SolutionScanResult
{
    public required string SolutionName { get; init; }
    public required string SolutionPath { get; init; }
    public DateTimeOffset ScannedAt { get; init; } = DateTimeOffset.Now;
    public List<ProjectScanResult> Projects { get; } = [];
    public List<ApiFinding> ApiFindings { get; } = [];
    public List<PackageCompatibilityFinding> PackageFindings { get; } = [];
    public List<SolidFinding> SolidFindings { get; } = [];
    public EconomicParameters? EconomicParameters { get; set; }
    public SolutionAdvisory? Advisory { get; set; }
    public ReportSummary Summary { get; set; } = new();
}

/// <summary>
/// Armazena os metadados e os achados associados a um projeto individual.
/// </summary>
public sealed class ProjectScanResult
{
    public required string ProjectName { get; init; }
    public required string ProjectPath { get; init; }
    public required IReadOnlyList<string> TargetFrameworks { get; init; }
    public required ProjectMigrationProfile MigrationProfile { get; init; }
    public required IReadOnlyList<PackageReferenceInfo> PackageReferences { get; init; }
    public required IReadOnlyList<string> AssemblyReferences { get; init; }
    public required IReadOnlyList<string> SourceFiles { get; init; }
}

/// <summary>
/// Resume a posiÃ§Ã£o de um projeto na jornada de migraÃ§Ã£o para .NET 10.
/// </summary>
public sealed class ProjectMigrationProfile
{
    public required string Classification { get; init; }
    public required string Impact { get; init; }
    public required string Summary { get; init; }
    public int Weight { get; init; }
}

/// <summary>
/// Representa uma dependÃªncia declarada diretamente no projeto ou em packages.config.
/// </summary>
public sealed class PackageReferenceInfo
{
    public required string PackageId { get; init; }
    public required string Version { get; init; }
    public string? PrivateAssets { get; init; }
    public bool IsFromPackagesConfig { get; init; }
}

/// <summary>
/// Define uma regra de detecÃ§Ã£o usada pelo scanner de APIs legadas.
/// </summary>
public sealed class ApiRule
{
    public required string Id { get; init; }
    public required string Api { get; init; }
    public required string Category { get; init; }
    public required string Impact { get; init; }
    public required string Effort { get; init; }
    public required string Alternative { get; init; }
    public required string Docs { get; init; }
    public string? Pattern { get; init; }
    public string? AppliesTo { get; init; }
    public string? PackageId { get; init; }
    public string? BusinessImpact { get; init; }
    public string? MonthlyInactionCost { get; init; }
    public EconomicProfile? EconomicProfile { get; init; }
}

/// <summary>
/// Representa uma ocorrÃªncia concreta de API legada encontrada no cÃ³digo-fonte.
/// </summary>
public sealed class ApiFinding
{
    public required string ProjectName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required ApiRule Rule { get; init; }
    public required string MatchedText { get; init; }
}

/// <summary>
/// Representa o diagnÃ³stico de compatibilidade de um pacote com o alvo .NET 10.
/// </summary>
public sealed class PackageCompatibilityFinding
{
    public required string ProjectName { get; init; }
    public required string PackageId { get; init; }
    public required string RequestedVersion { get; init; }
    public required string Status { get; init; }
    public required string Impact { get; init; }
    public required string Recommendation { get; init; }
    public required string Details { get; init; }
    public string? Effort { get; init; }
    public string? BusinessImpact { get; init; }
    public string? EstimatedMonthlyInactionCost { get; init; }
    public EconomicProfile? EconomicProfile { get; init; }
    public bool IsBlocker { get; init; }
    public bool IsWarning { get; init; }
}

/// <summary>
/// Define as premissas financeiras usadas para gerar ranges de custo orientativos no relatório.
/// </summary>
public sealed class EconomicParameters
{
    public required decimal HourlyRateMin { get; init; }
    public required decimal HourlyRateMax { get; init; }
    public required decimal WeeksPerMonth { get; init; }
    public required EconomicBand Low { get; init; }
    public required EconomicBand Medium { get; init; }
    public required EconomicBand High { get; init; }
    public string? Disclaimer { get; init; }
}

/// <summary>
/// Representa uma banda padrão de premissas para esforço, composição de equipe e exposição operacional.
/// </summary>
public sealed class EconomicBand
{
    public required decimal WeeklyHoursMin { get; init; }
    public required decimal WeeklyHoursMax { get; init; }
    public required decimal TeamSizeMin { get; init; }
    public required decimal TeamSizeMax { get; init; }
    public required decimal InfraCostMin { get; init; }
    public required decimal InfraCostMax { get; init; }
    public required decimal RiskMultiplierMin { get; init; }
    public required decimal RiskMultiplierMax { get; init; }
}

/// <summary>
/// Permite ajustar pontualmente uma regra com premissas econômicas próprias.
/// </summary>
public sealed class EconomicProfile
{
    public decimal? WeeklyHoursMin { get; init; }
    public decimal? WeeklyHoursMax { get; init; }
    public decimal? TeamSizeMin { get; init; }
    public decimal? TeamSizeMax { get; init; }
    public decimal? InfraCostMin { get; init; }
    public decimal? InfraCostMax { get; init; }
    public decimal? RiskMultiplierMin { get; init; }
    public decimal? RiskMultiplierMax { get; init; }
}

/// <summary>
/// Representa um range monetário mensal calculado a partir das premissas configuradas.
/// </summary>
public sealed class MonthlyCostRange
{
    public required decimal Min { get; init; }
    public required decimal Max { get; init; }
}

/// <summary>
/// Consolida os indicadores executivos exibidos no relatÃ³rio final.
/// </summary>
public sealed class ReportSummary
{
    public int ProjectsScanned { get; init; }
    public int CriticalBlockers { get; init; }
    public int Warnings { get; init; }
    public int InformationalItems { get; init; }
    public int RiskScore { get; init; }
    public MaintainabilityAssessment Maintainability { get; init; } = new()
    {
        Classification = "Não avaliado",
        ExecutiveSummary = "Sem avaliação de manutenibilidade."
    };
}

/// <summary>
/// Consolida a pontuação estrutural de manutenibilidade e seus vetores de composição.
/// </summary>
public sealed class MaintainabilityAssessment
{
    public int Score { get; init; }
    public required string Classification { get; init; } = "Não avaliado";
    public required string ExecutiveSummary { get; init; } = "Sem avaliação de manutenibilidade.";
    public MaintainabilityComponent MigrationRisk { get; init; } = new();
    public MaintainabilityComponent SolidDensity { get; init; } = new();
    public MaintainabilityComponent TechnologicalAge { get; init; } = new();
    public MaintainabilityComponent LegacyCoupling { get; init; } = new();
}

/// <summary>
/// Representa um componente individual da pontuação estrutural de manutenibilidade.
/// </summary>
public sealed class MaintainabilityComponent
{
    public string Name { get; init; } = string.Empty;
    public int RawScore { get; init; }
    public int WeightedScore { get; init; }
    public int WeightPercent { get; init; }
    public string Explanation { get; init; } = string.Empty;
}

/// <summary>
/// Representa uma leitura consultiva consolidada para apoiar decisão gerencial.
/// </summary>
public sealed class SolutionAdvisory
{
    public required string ExecutiveHeadline { get; init; }
    public required string ScenarioNarrative { get; init; }
    public required string RecommendedStrategy { get; init; }
    public required string Rationale { get; init; }
    public required string ManagerialPositioning { get; init; }
    public required string DistanceAssessment { get; init; }
    public required string OpportunitySummary { get; init; }
    public required IReadOnlyList<string> DecisionDrivers { get; init; }
    public required IReadOnlyList<DecisionPathOption> Paths { get; init; }
}

/// <summary>
/// Define um caminho possível de decisão com esforço relativo e quando faz sentido.
/// </summary>
public sealed class DecisionPathOption
{
    public required string Title { get; init; }
    public required string Fit { get; init; }
    public required string Effort { get; init; }
    public required string IndicativeRisk { get; init; }
    public required string Guidance { get; init; }
    public bool IsRecommended { get; init; }
}

/// <summary>
/// Representa um indício heurístico de violação de princípios SOLID em código-fonte C#.
/// </summary>
public sealed class SolidFinding
{
    public required string ProjectName { get; init; }
    public required string FilePath { get; init; }
    public required string Principle { get; init; }
    public required string Severity { get; init; }
    public required string Confidence { get; init; }
    public required string TargetName { get; init; }
    public required string Evidence { get; init; }
    public required string Explanation { get; init; }
    public required string Recommendation { get; init; }
    public int? LineNumber { get; init; }
}
