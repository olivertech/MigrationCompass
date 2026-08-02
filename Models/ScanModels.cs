namespace MigrationCompass.Models;

public sealed record ScanRequest(string SolutionPath, string OutputDirectory, string ReportFormat);

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

public sealed class ProjectMigrationProfile
{
    public required string Classification { get; init; }
    public required string Impact { get; init; }
    public required string Summary { get; init; }
    public int Weight { get; init; }
}

public sealed class PackageReferenceInfo
{
    public required string PackageId { get; init; }
    public required string Version { get; init; }
    public string? PrivateAssets { get; init; }
    public bool IsFromPackagesConfig { get; init; }
}

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
}

public sealed class ApiFinding
{
    public required string ProjectName { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required ApiRule Rule { get; init; }
    public required string MatchedText { get; init; }
}

public sealed class PackageCompatibilityFinding
{
    public required string ProjectName { get; init; }
    public required string PackageId { get; init; }
    public required string RequestedVersion { get; init; }
    public required string Status { get; init; }
    public required string Impact { get; init; }
    public required string Recommendation { get; init; }
    public required string Details { get; init; }
    public bool IsBlocker { get; init; }
    public bool IsWarning { get; init; }
}

public sealed class ReportSummary
{
    public int ProjectsScanned { get; init; }
    public int CriticalBlockers { get; init; }
    public int Warnings { get; init; }
    public int InformationalItems { get; init; }
    public int RiskScore { get; init; }
}