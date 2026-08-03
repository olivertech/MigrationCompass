using MigrationCompass.Models;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace MigrationCompass.Services;

/// <summary>
/// Avalia se os pacotes declarados pelos projetos sÃ£o compatÃ­veis com o alvo .NET 10.
/// </summary>
public sealed class NuGetChecker(INuGetPackageClient packageClient)
{
    private static readonly NuGetFramework TargetFramework = NuGetFramework.ParseFolder("net10.0");
    private readonly INuGetPackageClient _packageClient = packageClient;

    /// <summary>
    /// Executa a anÃ¡lise de todos os pacotes encontrados nos projetos da solution.
    /// </summary>
    public async Task<IReadOnlyList<PackageCompatibilityFinding>> CheckAsync(IEnumerable<ProjectScanResult> projects, CancellationToken cancellationToken)
    {
        var findings = new List<PackageCompatibilityFinding>();

        foreach (var project in projects)
        {
            foreach (var packageReference in project.PackageReferences)
            {
                if (ShouldSkip(packageReference.PackageId))
                {
                    continue;
                }

                findings.Add(await EvaluatePackageAsync(project.ProjectName, packageReference, cancellationToken));
            }
        }

        return findings;
    }

    /// <summary>
    /// Determina o status de um pacote individual, incluindo atualizaÃ§Ã£o compatÃ­vel, bloqueio ou fallback offline.
    /// </summary>
    private async Task<PackageCompatibilityFinding> EvaluatePackageAsync(string projectName, PackageReferenceInfo packageReference, CancellationToken cancellationToken)
    {
        if (string.Equals(packageReference.PrivateAssets, "all", StringComparison.OrdinalIgnoreCase))
        {
            return new PackageCompatibilityFinding
            {
                ProjectName = projectName,
                PackageId = packageReference.PackageId,
                RequestedVersion = packageReference.Version,
                Status = "Ignorado",
                Impact = "Informacao",
                Recommendation = "Dependencia marcada com PrivateAssets=all; revisar apenas se fizer parte da estrategia de empacotamento.",
                Details = "O pacote nao foi tratado como bloqueador por estar isolado do grafo publico do projeto.",
                IsBlocker = false,
                IsWarning = false
            };
        }

        try
        {
            var versions = await _packageClient.GetVersionsAsync(packageReference.PackageId, cancellationToken);
            var stableVersions = versions
                .Select(version => NuGetVersion.TryParse(version, out var parsed) ? parsed : null)
                .Where(version => version is not null && !version.IsPrerelease)
                .Cast<NuGetVersion>()
                .OrderByDescending(version => version)
                .ToArray();

            var requestedVersion = NuGetVersion.TryParse(packageReference.Version, out var parsedRequestedVersion)
                ? parsedRequestedVersion
                : stableVersions.FirstOrDefault();

            var requestedIsCompatible = requestedVersion is not null &&
                await IsCompatibleAsync(packageReference.PackageId, requestedVersion.ToNormalizedString(), cancellationToken);

            if (requestedIsCompatible)
            {
                return new PackageCompatibilityFinding
                {
                    ProjectName = projectName,
                    PackageId = packageReference.PackageId,
                    RequestedVersion = packageReference.Version,
                    Status = "Compativel",
                    Impact = "Informacao",
                    Recommendation = $"Manter {packageReference.PackageId} na versao atual ou superior, preservando testes de regressao.",
                    Details = $"A versao {requestedVersion!.ToNormalizedString()} possui assets compativeis com .NET 10.",
                    IsBlocker = false,
                    IsWarning = false
                };
            }

            foreach (var version in stableVersions)
            {
                if (await IsCompatibleAsync(packageReference.PackageId, version.ToNormalizedString(), cancellationToken))
                {
                    return new PackageCompatibilityFinding
                    {
                        ProjectName = projectName,
                        PackageId = packageReference.PackageId,
                        RequestedVersion = packageReference.Version,
                        Status = "Compativel com atualizacao",
                        Impact = "Medio",
                        Recommendation = $"Atualizar para {version.ToNormalizedString()} antes da migracao para .NET 10.",
                        Details = $"A versao atual nao expoe assets compativeis, mas {version.ToNormalizedString()} oferece suporte melhor ao alvo.",
                        IsBlocker = false,
                        IsWarning = true
                    };
                }
            }

            return new PackageCompatibilityFinding
            {
                ProjectName = projectName,
                PackageId = packageReference.PackageId,
                RequestedVersion = packageReference.Version,
                Status = "BLOQUEADOR",
                Impact = "Alto",
                Recommendation = $"Nenhuma versao estavel compativel com .NET 10 foi encontrada para {packageReference.PackageId}. Avaliar substituicao.",
                Details = "Foram avaliadas as versoes estaveis publicadas no feed oficial do NuGet, sem encontrar assets adequados para .NET 10 ou .NET Standard.",
                IsBlocker = true,
                IsWarning = false
            };
        }
        catch (Exception ex) when (IsOfflineScenario(ex))
        {
            return new PackageCompatibilityFinding
            {
                ProjectName = projectName,
                PackageId = packageReference.PackageId,
                RequestedVersion = packageReference.Version,
                Status = "Nao verificado offline",
                Impact = "Medio",
                Recommendation = "Executar novamente em ambiente com acesso ao api.nuget.org para confirmar compatibilidade do pacote.",
                Details = $"A validacao remota nao foi concluida: {ex.Message}",
                IsBlocker = false,
                IsWarning = true
            };
        }
    }

    /// <summary>
    /// Verifica se uma versÃ£o especÃ­fica do pacote expÃµe assets compatÃ­veis com .NET 10.
    /// </summary>
    private async Task<bool> IsCompatibleAsync(string packageId, string version, CancellationToken cancellationToken)
    {
        var frameworks = await _packageClient.GetAssetFrameworkFoldersAsync(packageId, version, cancellationToken);
        if (frameworks.Count == 0)
        {
            return false;
        }

        foreach (var folder in frameworks)
        {
            NuGetFramework packageFramework;
            try
            {
                packageFramework = NuGetFramework.ParseFolder(folder);
            }
            catch
            {
                continue;
            }

            if (DefaultCompatibilityProvider.Instance.IsCompatible(TargetFramework, packageFramework))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Ignora frameworks compartilhados cujo ciclo de compatibilidade jÃ¡ Ã© coberto pelo prÃ³prio TFM do projeto.
    /// </summary>
    private static bool ShouldSkip(string packageId)
    {
        return packageId.StartsWith("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase) ||
               packageId.StartsWith("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Classifica erros de rede como cenÃ¡rios offline tolerÃ¡veis para o scanner.
    /// </summary>
    private static bool IsOfflineScenario(Exception exception)
    {
        if (exception is HttpRequestException)
        {
            return true;
        }

        return exception.InnerException is HttpRequestException;
    }
}