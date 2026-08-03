using System.Reflection;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Descobre os projetos de uma solution e extrai os metadados necessarios para a analise de migracao.
/// </summary>
public sealed class SolutionScanner
{
    /// <summary>
    /// Executa a leitura da solution, carregando projetos via MSBuild quando possivel e usando fallback em XML quando necessario.
    /// </summary>
    public async Task<SolutionScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        MsBuildEnvironment.Configure();

        var preferXmlFallback = IsSingleFileExecution();
        var solution = SolutionFile.Parse(request.SolutionPath);
        var solutionDirectory = Path.GetDirectoryName(request.SolutionPath) ?? Directory.GetCurrentDirectory();
        var globalProperties = MsBuildEnvironment.CreateGlobalProperties();

        var result = new SolutionScanResult
        {
            SolutionName = Path.GetFileNameWithoutExtension(request.SolutionPath),
            SolutionPath = request.SolutionPath
        };

        var projectEntries = solution.ProjectsInOrder
            .Where(project => project.AbsolutePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase));

        foreach (var projectEntry in projectEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var projectPath = projectEntry.AbsolutePath;
            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.GetFullPath(Path.Combine(solutionDirectory, projectEntry.RelativePath));
            }

            IReadOnlyList<string> targetFrameworks;
            IReadOnlyList<PackageReferenceInfo> packages;
            IReadOnlyList<string> references;

            if (preferXmlFallback)
            {
                var fallback = ParseProjectWithoutEvaluation(projectPath);
                targetFrameworks = fallback.TargetFrameworks;
                packages = fallback.PackageReferences;
                references = fallback.References;
            }
            else
            {
                try
                {
                    using var projectCollection = new ProjectCollection();
                    var project = projectCollection.LoadProject(projectPath, globalProperties, "Current");
                    targetFrameworks = ExtractTargetFrameworks(project);
                    packages = ExtractPackageReferences(projectPath, project);
                    references = project.GetItems("Reference")
                        .Select(item => item.EvaluatedInclude)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                }
                catch (Exception)
                {
                    var fallback = ParseProjectWithoutEvaluation(projectPath);
                    targetFrameworks = fallback.TargetFrameworks;
                    packages = fallback.PackageReferences;
                    references = fallback.References;
                }
            }

            var profile = ProjectClassification.Classify(targetFrameworks);
            var sourceFiles = Directory.GetFiles(Path.GetDirectoryName(projectPath)!, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            result.Projects.Add(new ProjectScanResult
            {
                ProjectName = Path.GetFileNameWithoutExtension(projectPath),
                ProjectPath = projectPath,
                TargetFrameworks = targetFrameworks,
                MigrationProfile = profile,
                PackageReferences = packages,
                AssemblyReferences = references,
                SourceFiles = sourceFiles
            });
        }

        return result;
    }

    /// <summary>
    /// Extrai TFMs simples ou multiplos a partir do projeto ja avaliado pelo MSBuild.
    /// </summary>
    private static IReadOnlyList<string> ExtractTargetFrameworks(Project project)
    {
        var single = project.GetPropertyValue("TargetFramework");
        if (!string.IsNullOrWhiteSpace(single))
        {
            return [single.Trim()];
        }

        var multiple = project.GetPropertyValue("TargetFrameworks");
        if (!string.IsNullOrWhiteSpace(multiple))
        {
            return multiple.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        var legacyFramework = NormalizeLegacyFramework(
            project.GetPropertyValue("TargetFrameworkVersion"),
            project.GetPropertyValue("TargetFrameworkProfile"));

        if (!string.IsNullOrWhiteSpace(legacyFramework))
        {
            return [legacyFramework];
        }

        if (string.IsNullOrWhiteSpace(multiple))
        {
            return ["desconhecido"];
        }

        return ["desconhecido"];
    }

    /// <summary>
    /// Coleta dependencias diretas declaradas no projeto e em packages.config.
    /// </summary>
    private static IReadOnlyList<PackageReferenceInfo> ExtractPackageReferences(string projectPath, Project project)
    {
        var packages = new List<PackageReferenceInfo>();

        foreach (var item in project.GetItems("PackageReference"))
        {
            packages.Add(new PackageReferenceInfo
            {
                PackageId = item.EvaluatedInclude,
                Version = item.GetMetadataValue("Version"),
                PrivateAssets = item.GetMetadataValue("PrivateAssets"),
                IsFromPackagesConfig = false
            });
        }

        var packagesConfigPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.config");
        if (File.Exists(packagesConfigPath))
        {
            var document = XDocument.Load(packagesConfigPath);
            foreach (var package in document.Root?.Elements("package") ?? [])
            {
                packages.Add(new PackageReferenceInfo
                {
                    PackageId = package.Attribute("id")?.Value ?? string.Empty,
                    Version = package.Attribute("version")?.Value ?? string.Empty,
                    PrivateAssets = null,
                    IsFromPackagesConfig = true
                });
            }
        }

        return packages
            .Where(package => !string.IsNullOrWhiteSpace(package.PackageId))
            .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    /// <summary>
    /// Usa leitura XML direta quando a avaliacao completa do projeto nao e possivel no runtime atual.
    /// </summary>
    private static FallbackProjectData ParseProjectWithoutEvaluation(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        var root = document.Root ?? throw new InvalidOperationException($"Projeto invalido: {projectPath}");
        var ns = root.Name.Namespace;

        var targetFramework = root
            .Descendants(ns + "TargetFramework")
            .Select(element => element.Value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var targetFrameworks = root
            .Descendants(ns + "TargetFrameworks")
            .SelectMany(element => element.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        var legacyFrameworkVersion = root
            .Descendants(ns + "TargetFrameworkVersion")
            .Select(element => NormalizeLegacyFramework(
                element.Value,
                root.Descendants(ns + "TargetFrameworkProfile").Select(profile => profile.Value).FirstOrDefault()))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray();

        var frameworks = targetFramework
            .Concat(targetFrameworks)
            .Concat(legacyFrameworkVersion)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (frameworks.Length == 0)
        {
            frameworks = ["desconhecido"];
        }

        var packages = root
            .Descendants(ns + "PackageReference")
            .Select(element => new PackageReferenceInfo
            {
                PackageId = element.Attribute("Include")?.Value ?? element.Attribute("Update")?.Value ?? string.Empty,
                Version = element.Attribute("Version")?.Value ?? element.Element(ns + "Version")?.Value ?? string.Empty,
                PrivateAssets = element.Attribute("PrivateAssets")?.Value ?? element.Element(ns + "PrivateAssets")?.Value,
                IsFromPackagesConfig = false
            })
            .Where(package => !string.IsNullOrWhiteSpace(package.PackageId))
            .ToList();

        var references = root
            .Descendants(ns + "Reference")
            .Select(element => element.Attribute("Include")?.Value ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var packagesConfigPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.config");
        if (File.Exists(packagesConfigPath))
        {
            var packagesConfig = XDocument.Load(packagesConfigPath);
            foreach (var package in packagesConfig.Root?.Elements("package") ?? [])
            {
                packages.Add(new PackageReferenceInfo
                {
                    PackageId = package.Attribute("id")?.Value ?? string.Empty,
                    Version = package.Attribute("version")?.Value ?? string.Empty,
                    PrivateAssets = null,
                    IsFromPackagesConfig = true
                });
            }
        }

        return new FallbackProjectData(
            frameworks,
            packages
                .GroupBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(package => package.PackageId, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            references);
    }

    private static bool IsSingleFileExecution()
    {
        var assemblyLocation = Assembly.GetExecutingAssembly().Location;
        return string.IsNullOrWhiteSpace(assemblyLocation) || !File.Exists(assemblyLocation);
    }

    private static string? NormalizeLegacyFramework(string? version, string? profile)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var normalizedVersion = version.Trim().TrimStart('v', 'V');
        var moniker = normalizedVersion switch
        {
            "3.0" => "net30",
            "3.5" => "net35",
            "4.0" => "net40",
            "4.5" => "net45",
            "4.5.1" => "net451",
            "4.5.2" => "net452",
            "4.6" => "net46",
            "4.6.1" => "net461",
            "4.6.2" => "net462",
            "4.7" => "net47",
            "4.7.1" => "net471",
            "4.7.2" => "net472",
            "4.8" => "net48",
            "4.8.1" => "net481",
            _ => null
        };

        if (moniker is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(profile))
        {
            return moniker;
        }

        var normalizedProfile = profile.Trim().Replace(" ", string.Empty).ToLowerInvariant();
        return $"{moniker}-{normalizedProfile}";
    }

    private sealed record FallbackProjectData(
        IReadOnlyList<string> TargetFrameworks,
        IReadOnlyList<PackageReferenceInfo> PackageReferences,
        IReadOnlyList<string> References);
}
