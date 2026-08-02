namespace MigrationCompass.Services;

public interface INuGetPackageClient
{
    Task<IReadOnlyList<string>> GetVersionsAsync(string packageId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetAssetFrameworkFoldersAsync(string packageId, string version, CancellationToken cancellationToken);
}