using MigrationCompass.Models;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace MigrationCompass.Services;

/// <summary>
/// Avalia se os pacotes declarados pelos projetos sao compativeis com o alvo .NET 10
/// e destaca apenas dependencias com impacto real na migracao de runtime.
/// </summary>
public sealed class NuGetChecker(INuGetPackageClient packageClient, IReadOnlyList<ApiRule> rules, IReadOnlySet<string>? irrelevantPackages = null)
{
    private static readonly NuGetFramework TargetFramework = NuGetFramework.ParseFolder("net10.0");
    private readonly INuGetPackageClient _packageClient = packageClient;
    private readonly IReadOnlyList<ApiRule> _packageRules = rules
        .Where(rule => string.Equals(rule.AppliesTo, "package", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    private readonly IReadOnlySet<string> _irrelevantPackages = irrelevantPackages ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Executa a analise de todos os pacotes relevantes encontrados nos projetos da solution.
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
    /// Determina o status de um pacote individual, incluindo compatibilidade, bloqueio ou fallback offline.
    /// </summary>
    private async Task<PackageCompatibilityFinding> EvaluatePackageAsync(string projectName, PackageReferenceInfo packageReference, CancellationToken cancellationToken)
    {
        var matchingRule = FindPackageRule(packageReference.PackageId);

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
                Effort = "Baixo",
                BusinessImpact = null,
                EstimatedMonthlyInactionCost = null,
                EconomicProfile = matchingRule?.EconomicProfile,
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
                    Effort = "Baixo",
                    BusinessImpact = null,
                    EstimatedMonthlyInactionCost = null,
                    EconomicProfile = matchingRule?.EconomicProfile,
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
                        Impact = matchingRule?.Impact ?? "Medio",
                        Recommendation = $"Atualizar para {version.ToNormalizedString()} antes da migracao para .NET 10.",
                        Details = $"A versao atual nao expoe assets compativeis, mas {version.ToNormalizedString()} oferece suporte melhor ao alvo.",
                        Effort = matchingRule?.Effort ?? "Medio",
                        BusinessImpact = matchingRule?.BusinessImpact,
                        EstimatedMonthlyInactionCost = matchingRule?.MonthlyInactionCost,
                        EconomicProfile = matchingRule?.EconomicProfile,
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
                Impact = matchingRule?.Impact ?? "Alto",
                Recommendation = matchingRule?.Alternative ?? $"Nenhuma versao estavel compativel com .NET 10 foi encontrada para {packageReference.PackageId}. Avaliar substituicao.",
                Details = "Foram avaliadas as versoes estaveis publicadas no feed oficial do NuGet, sem encontrar assets adequados para .NET 10 ou .NET Standard.",
                Effort = matchingRule?.Effort ?? "Medio",
                BusinessImpact = matchingRule?.BusinessImpact ?? BuildGenericBusinessImpact(packageReference.PackageId),
                EstimatedMonthlyInactionCost = matchingRule?.MonthlyInactionCost ?? "A estimar apos discovery tecnico",
                EconomicProfile = matchingRule?.EconomicProfile,
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
                Effort = matchingRule?.Effort ?? "Baixo",
                BusinessImpact = matchingRule?.BusinessImpact,
                EstimatedMonthlyInactionCost = matchingRule?.MonthlyInactionCost,
                EconomicProfile = matchingRule?.EconomicProfile,
                IsBlocker = false,
                IsWarning = true
            };
        }
    }

    /// <summary>
    /// Verifica se uma versao especifica do pacote expoe assets compativeis com .NET 10.
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
    /// Ignora pacotes client-side, ferramentas de build e frameworks compartilhados que nao bloqueiam runtime.
    /// </summary>
    private bool ShouldSkip(string packageId)
    {
        return packageId.StartsWith("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase) ||
               packageId.StartsWith("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase) ||
               _irrelevantPackages.Contains(packageId);
    }

    private ApiRule? FindPackageRule(string packageId)
    {
        return _packageRules.FirstOrDefault(rule =>
            string.Equals(rule.PackageId, packageId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rule.Api, packageId, StringComparison.OrdinalIgnoreCase) ||
            MatchesWildcard(rule.PackageId, packageId) ||
            MatchesWildcard(rule.Api, packageId));
    }

    private static bool MatchesWildcard(string? pattern, string packageId)
    {
        if (string.IsNullOrWhiteSpace(pattern) || !pattern.Contains('*', StringComparison.Ordinal))
        {
            return false;
        }

        var normalizedPattern = pattern.Trim();
        if (normalizedPattern == "*")
        {
            return true;
        }

        var segments = normalizedPattern.Split('*', StringSplitOptions.None);
        var index = 0;

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
            {
                continue;
            }

            var foundIndex = packageId.IndexOf(segment, index, StringComparison.OrdinalIgnoreCase);
            if (foundIndex < 0)
            {
                return false;
            }

            if (index == 0 && !normalizedPattern.StartsWith('*') && foundIndex != 0)
            {
                return false;
            }

            index = foundIndex + segment.Length;
        }

        if (!normalizedPattern.EndsWith('*'))
        {
            var lastSegment = segments.LastOrDefault(static segment => !string.IsNullOrEmpty(segment));
            return lastSegment is null || packageId.EndsWith(lastSegment, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }

    private static string BuildGenericBusinessImpact(string packageId)
    {
        return $"A dependencia {packageId} nao apresentou trilha clara de compatibilidade com .NET 10. Manter esse pacote aumenta risco de retrabalho, testes adicionais e atrasos na liberacao da migracao em producao.";
    }

    /// <summary>
    /// Classifica erros de rede como cenarios offline toleraveis para o scanner.
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
