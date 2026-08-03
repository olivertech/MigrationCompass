using System.Text.RegularExpressions;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Aplica as regras de APIs legadas aos arquivos .cs de cada projeto.
/// </summary>
public sealed class ApiScanner
{
    private readonly IReadOnlyList<(ApiRule Rule, Regex Regex)> _compiledRules;

    public ApiScanner(IReadOnlyList<ApiRule> rules)
    {
        _compiledRules = rules
            .Select(rule => (rule, new Regex(rule.Pattern ?? Regex.Escape(rule.Api).Replace("\\*", ".*"), RegexOptions.Compiled)))
            .ToArray();
    }

    /// <summary>
    /// Percorre os arquivos-fonte e registra cada ocorrÃªncia das regras configuradas.
    /// </summary>
    public async Task<IReadOnlyList<ApiFinding>> ScanAsync(IEnumerable<ProjectScanResult> projects, CancellationToken cancellationToken)
    {
        var findings = new List<ApiFinding>();

        foreach (var project in projects)
        {
            foreach (var sourceFile in project.SourceFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var lines = await File.ReadAllLinesAsync(sourceFile, cancellationToken);
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    foreach (var (rule, regex) in _compiledRules)
                    {
                        var matches = regex.Matches(line);
                        foreach (Match match in matches)
                        {
                            findings.Add(new ApiFinding
                            {
                                ProjectName = project.ProjectName,
                                FilePath = sourceFile,
                                LineNumber = index + 1,
                                Rule = rule,
                                MatchedText = match.Value
                            });
                        }
                    }
                }
            }
        }

        return findings;
    }
}