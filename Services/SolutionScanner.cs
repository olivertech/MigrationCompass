using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

public sealed class SolutionScanner
{
    public async Task<SolutionScanResult> ScanAsync(ScanRequest request, CancellationToken cancellationToken)
    {
        await Task.Yield();
        MsBuildEnvironment.Configure();

        var solution = SolutionFile.Parse(request.SolutionPath);
        var solutionDirectory = Path.GetDirectoryName(request.SolutionPath) ?? Directory.GetCurrentDirectory();
        var projectCollection = new ProjectCollection();
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

            try
            {
                var project = projectCollection.LoadProject(projectPath, globalProperties, "Current");
                targetFrameworks = ExtractTargetFrameworks(project);
                packages = ExtractPackageReferences(projectPath, project);
                references = project.GetItems("Reference")
                    .Select(item => item.EvaluatedInclude)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (InvalidProjectFileException)
            {
                var fallback = ParseProjectWithoutEvaluation(projectPath);
                targetFrameworks = fallback.TargetFrameworks;
                packages = fallback.PackageReferences;
                references = fallback.References;
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

    private static IReadOnlyList<string> ExtractTargetFrameworks(Project project)
    {
        var single = project.GetPropertyValue("TargetFramework");
        if (!string.IsNullOrWhiteSpace(single))
        {
            return [single.Trim()];
        }

        var multiple = project.GetPropertyValue("TargetFrameworks");
        if (string.IsNullOrWhiteSpace(multiple))
        {
            return ["desconhecido"];
        }

        return multiple.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

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

        var frameworks = targetFramework.Concat(targetFrameworks).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
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

    private sealed record FallbackProjectData(
        IReadOnlyList<string> TargetFrameworks,
        IReadOnlyList<PackageReferenceInfo> PackageReferences,
        IReadOnlyList<string> References);
}