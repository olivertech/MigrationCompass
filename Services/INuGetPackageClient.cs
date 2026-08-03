namespace MigrationCompass.Services;

/// <summary>
/// Abstrai o acesso Ã s informaÃ§Ãµes remotas de pacotes para facilitar testes e fallback de implementaÃ§Ã£o.
/// </summary>
public interface INuGetPackageClient
{
    Task<IReadOnlyList<string>> GetVersionsAsync(string packageId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetAssetFrameworkFoldersAsync(string packageId, string version, CancellationToken cancellationToken);
}