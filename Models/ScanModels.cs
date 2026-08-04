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
    public bool IsBlocker { get; init; }
    public bool IsWarning { get; init; }
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
}
