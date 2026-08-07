using System.Text.RegularExpressions;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Executa uma análise heurística de sinais de não conformidade com princípios SOLID em arquivos C#.
/// </summary>
public sealed class SolidScanner
{
    private static readonly Regex ClassRegex = new(@"(?<mods>(public|internal|protected|private|abstract|sealed|static|partial)\s+)*(class|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex InterfaceRegex = new(@"(?<mods>(public|internal|protected|private|partial)\s+)*interface\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", RegexOptions.Compiled);
    private static readonly Regex MethodRegex = new(@"(?<mods>(public|private|protected|internal|static|virtual|override|async|sealed|new|partial)\s+)+(?<ret>[A-Za-z_<>\[\]\?,\.]+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\((?<params>[^\)]*)\)\s*(\{|=>)", RegexOptions.Compiled);
    private static readonly Regex InterfaceMemberRegex = new(@"^\s*([A-Za-z_<>\[\]\?,\.]+\s+)?[A-Za-z_][A-Za-z0-9_]*\s*\([^\)]*\)\s*;\s*$|^\s*[A-Za-z_<>\[\]\?,\.]+\s+[A-Za-z_][A-Za-z0-9_]*\s*\{\s*get\s*;\s*(set\s*;\s*)?\}\s*$", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex PropertyRegex = new(@"(?<mods>(public|private|protected|internal|virtual|override|static)\s+)+(?<type>[A-Za-z_<>\[\]\?,\.]+)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\{\s*get\s*;?", RegexOptions.Compiled);
    private static readonly Regex NewRegex = new(@"\bnew\s+[A-Za-z_][A-Za-z0-9_\<\>\.\[\],]*\s*\(", RegexOptions.Compiled);
    private static readonly Regex SwitchRegex = new(@"\bswitch\s*\(", RegexOptions.Compiled);
    private static readonly Regex ElseIfRegex = new(@"\belse\s+if\s*\(", RegexOptions.Compiled);
    private static readonly Regex OverrideRegex = new(@"\boverride\b", RegexOptions.Compiled);
    private static readonly Regex ThrowNotSupportedRegex = new(@"\bthrow\s+new\s+(NotSupportedException|NotImplementedException)\b", RegexOptions.Compiled);
    private static readonly Regex DependencyFieldRegex = new(@"private\s+(readonly\s+)?[A-Za-z_][A-Za-z0-9_<>\[\]\?,\.]*\s+_[A-Za-z_][A-Za-z0-9_]*\s*;", RegexOptions.Compiled);

    /// <summary>
    /// Analisa os arquivos de código dos projetos e registra indícios de violações SOLID.
    /// </summary>
    public async Task<IReadOnlyList<SolidFinding>> ScanAsync(IEnumerable<ProjectScanResult> projects, CancellationToken cancellationToken)
    {
        var findings = new List<SolidFinding>();

        foreach (var project in projects)
        {
            foreach (var sourceFile in project.SourceFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!sourceFile.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var content = await File.ReadAllTextAsync(sourceFile, cancellationToken);
                var lines = await File.ReadAllLinesAsync(sourceFile, cancellationToken);

                findings.AddRange(AnalyzeFile(project.ProjectName, sourceFile, content, lines));
            }
        }

        return findings
            .OrderByDescending(finding => SeverityWeight(finding.Severity))
            .ThenBy(finding => finding.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<SolidFinding> AnalyzeFile(string projectName, string filePath, string content, string[] lines)
    {
        var findings = new List<SolidFinding>();
        var classMatches = ClassRegex.Matches(content).Cast<Match>().ToArray();
        var interfaceMatches = InterfaceRegex.Matches(content).Cast<Match>().ToArray();
        var methodMatches = MethodRegex.Matches(content).Cast<Match>().ToArray();

        foreach (var classMatch in classMatches)
        {
            var className = classMatch.Groups["name"].Value;
            var classLine = GetLineNumber(content, classMatch.Index);
            var classBlock = ExtractBlock(content, classMatch.Index);
            if (classBlock.Length == 0)
            {
                continue;
            }

            var classLineCount = CountLines(classBlock);
            var methodCount = MethodRegex.Matches(classBlock).Count;
            var dependencyFields = DependencyFieldRegex.Matches(classBlock).Count;
            var newCount = NewRegex.Matches(classBlock).Count;
            var switchCount = SwitchRegex.Matches(classBlock).Count;
            var elseIfCount = ElseIfRegex.Matches(classBlock).Count;
            var constructorParams = CountConstructorParameters(className, classBlock);

            if (classLineCount >= 400 || methodCount >= 15)
            {
                findings.Add(new SolidFinding
                {
                    ProjectName = projectName,
                    FilePath = filePath,
                    Principle = "SRP",
                    Severity = classLineCount >= 700 || methodCount >= 25 ? "Alto" : "Medio",
                    Confidence = "Baixa",
                    TargetName = className,
                    LineNumber = classLine,
                    Evidence = $"Classe com {classLineCount} linha(s) e {methodCount} método(s).",
                    Explanation = "Classes muito extensas ou com muitos comportamentos distintos costumam concentrar múltiplas responsabilidades, dificultando teste, manutenção e evolução.",
                    Recommendation = "Avaliar fatiamento por responsabilidade de negócio, separando orquestração, regras, infraestrutura e integração."
                });
            }

            if (switchCount >= 2 || elseIfCount >= 4)
            {
                findings.Add(new SolidFinding
                {
                    ProjectName = projectName,
                    FilePath = filePath,
                    Principle = "OCP",
                    Severity = switchCount >= 4 || elseIfCount >= 8 ? "Alto" : "Medio",
                    Confidence = "Baixa",
                    TargetName = className,
                    LineNumber = classLine,
                    Evidence = BuildJoinedEvidence(
                        "Encontrado(s) ",
                        switchCount > 0 ? $"{switchCount} bloco(s) switch" : null,
                        elseIfCount > 0 ? $"{elseIfCount} encadeamento(s) else-if" : null),
                    Explanation = "Fluxos baseados em decisões por tipo ou condição extensa costumam exigir alteração frequente da classe sempre que um novo comportamento é introduzido.",
                    Recommendation = "Avaliar uso de polimorfismo, estratégias ou handlers especializados para reduzir modificação recorrente da mesma classe."
                });
            }

            if (newCount >= 6 || constructorParams >= 6 || dependencyFields >= 6)
            {
                findings.Add(new SolidFinding
                {
                    ProjectName = projectName,
                    FilePath = filePath,
                    Principle = "DIP",
                    Severity = constructorParams >= 8 || newCount >= 10 ? "Alto" : "Medio",
                    Confidence = "Baixa",
                    TargetName = className,
                    LineNumber = classLine,
                    Evidence = BuildJoinedEvidence(
                        string.Empty,
                        constructorParams > 0 ? $"Construtor com {constructorParams} parâmetro(s)" : null,
                        dependencyFields > 0 ? $"{dependencyFields} campo(s) de dependência" : null,
                        newCount > 0 ? $"{newCount} instanciação(ões) direta(s)" : null),
                    Explanation = "Muitas dependências concretas ou criação direta de objetos dentro da classe sugerem acoplamento excessivo a detalhes de implementação.",
                    Recommendation = "Revisar fronteiras de dependência, abstrações e composição via DI para reduzir acoplamento e facilitar substituição."
                });
            }
        }

        foreach (var methodMatch in methodMatches)
        {
            var methodName = methodMatch.Groups["name"].Value;
            var methodLine = GetLineNumber(content, methodMatch.Index);
            var methodBlock = ExtractBlock(content, methodMatch.Index);
            if (methodBlock.Length == 0)
            {
                continue;
            }

            var lineCount = CountLines(methodBlock);
            var newCount = NewRegex.Matches(methodBlock).Count;
            var switchCount = SwitchRegex.Matches(methodBlock).Count;

            if (lineCount >= 80)
            {
                findings.Add(new SolidFinding
                {
                    ProjectName = projectName,
                    FilePath = filePath,
                    Principle = "SRP",
                    Severity = lineCount >= 150 ? "Alto" : "Medio",
                    Confidence = "Baixa",
                    TargetName = methodName,
                    LineNumber = methodLine,
                    Evidence = $"Método com aproximadamente {lineCount} linha(s).",
                    Explanation = "Métodos muito extensos tendem a misturar coordenação, regra de negócio, persistência e tratamento operacional no mesmo fluxo.",
                    Recommendation = "Extrair etapas do processamento em métodos ou serviços menores com responsabilidades mais nítidas."
                });
            }

            if (newCount >= 4 && switchCount >= 1)
            {
                findings.Add(new SolidFinding
                {
                    ProjectName = projectName,
                    FilePath = filePath,
                    Principle = "DIP",
                    Severity = "Medio",
                    Confidence = "Baixa",
                    TargetName = methodName,
                    LineNumber = methodLine,
                    Evidence = BuildJoinedEvidence(
                        string.Empty,
                        newCount > 0 ? $"Método com {newCount} instanciação(ões) direta(s)" : null,
                        switchCount > 0 ? $"{switchCount} decisão(ões) por fluxo" : null),
                    Explanation = "Métodos que instanciam muitos tipos concretos e controlam múltiplos fluxos tendem a ficar fortemente acoplados a detalhes e difíceis de substituir.",
                    Recommendation = "Avaliar extração de fábricas, estratégias ou adapters para reduzir dependência direta de implementações concretas."
                });
            }
        }

        foreach (var interfaceMatch in interfaceMatches)
        {
            var interfaceName = interfaceMatch.Groups["name"].Value;
            var interfaceLine = GetLineNumber(content, interfaceMatch.Index);
            var interfaceBlock = ExtractBlock(content, interfaceMatch.Index);
            if (interfaceBlock.Length == 0)
            {
                continue;
            }

            var memberCount = InterfaceMemberRegex.Matches(interfaceBlock).Count;
            if (memberCount >= 8)
            {
                findings.Add(new SolidFinding
                {
                    ProjectName = projectName,
                    FilePath = filePath,
                    Principle = "ISP",
                    Severity = memberCount >= 15 ? "Alto" : "Medio",
                    Confidence = "Media",
                    TargetName = interfaceName,
                    LineNumber = interfaceLine,
                    Evidence = $"Interface com {memberCount} membro(s).",
                    Explanation = "Interfaces extensas aumentam a chance de consumidores dependerem de contratos maiores do que realmente precisam.",
                    Recommendation = "Avaliar segmentação da interface por casos de uso, capacidade ou perfil de consumidor."
                });
            }
        }

        if (OverrideRegex.IsMatch(content) && ThrowNotSupportedRegex.IsMatch(content))
        {
            findings.Add(new SolidFinding
            {
                ProjectName = projectName,
                FilePath = filePath,
                Principle = "LSP",
                Severity = "Medio",
                Confidence = "Baixa",
                TargetName = Path.GetFileNameWithoutExtension(filePath),
                LineNumber = GetFirstMatchingLine(lines, ThrowNotSupportedRegex),
                Evidence = "Foram encontrados override(s) em conjunto com exceções como NotSupportedException ou NotImplementedException.",
                Explanation = "Esse padrão pode indicar substituições frágeis entre tipos derivados e base, especialmente quando comportamentos esperados deixam de ser suportados em subclasses.",
                Recommendation = "Revisar a hierarquia de herança e avaliar se a especialização deveria ser tratada por composição, estratégia ou contratos mais específicos."
            });
        }

        return findings;
    }

    private static int CountConstructorParameters(string className, string classBlock)
    {
        var pattern = $@"(?<mods>(public|private|protected|internal)\s+)+{Regex.Escape(className)}\s*\((?<params>[^\)]*)\)\s*(\{{|:)";
        var regex = new Regex(pattern, RegexOptions.Compiled);
        var match = regex.Match(classBlock);
        if (!match.Success)
        {
            return 0;
        }

        return CountParameters(match.Groups["params"].Value);
    }

    private static int CountParameters(string parameters)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return 0;
        }

        return parameters
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Count(part => !string.IsNullOrWhiteSpace(part));
    }

    private static string BuildJoinedEvidence(string prefix, params string?[] parts)
    {
        var relevantParts = parts
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        var content = string.Join(", ", relevantParts[..Math.Max(relevantParts.Length - 1, 0)]);
        if (relevantParts.Length > 1)
        {
            content = string.IsNullOrWhiteSpace(content)
                ? relevantParts[^1]!
                : $"{content} e {relevantParts[^1]}";
        }
        else if (relevantParts.Length == 1)
        {
            content = relevantParts[0]!;
        }

        return string.IsNullOrWhiteSpace(prefix)
            ? $"{content}."
            : $"{prefix}{content}.";
    }

    private static string ExtractBlock(string content, int startIndex)
    {
        var openBraceIndex = content.IndexOf('{', startIndex);
        if (openBraceIndex < 0)
        {
            return string.Empty;
        }

        var depth = 0;
        for (var index = openBraceIndex; index < content.Length; index++)
        {
            if (content[index] == '{')
            {
                depth++;
            }
            else if (content[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return content[startIndex..(index + 1)];
                }
            }
        }

        return content[startIndex..];
    }

    private static int CountLines(string block)
    {
        return block.Split('\n').Length;
    }

    private static int GetLineNumber(string content, int index)
    {
        return content[..Math.Min(index, content.Length)].Count(character => character == '\n') + 1;
    }

    private static int? GetFirstMatchingLine(string[] lines, Regex regex)
    {
        for (var index = 0; index < lines.Length; index++)
        {
            if (regex.IsMatch(lines[index]))
            {
                return index + 1;
            }
        }

        return null;
    }

    private static int SeverityWeight(string severity)
    {
        return severity.Trim().ToLowerInvariant() switch
        {
            "alto" => 3,
            "medio" => 2,
            "médio" => 2,
            _ => 1
        };
    }
}
