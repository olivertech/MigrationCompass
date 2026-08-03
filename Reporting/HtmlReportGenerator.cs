using System.Text;
using System.Text.Encodings.Web;
using MigrationCompass.Models;

namespace MigrationCompass.Reporting;

/// <summary>
/// Gera o relatÃ³rio HTML autocontido apresentado ao pÃºblico tÃ©cnico e executivo.
/// </summary>
public sealed class HtmlReportGenerator
{
    private const string AppVersion = "v3.0.0";

    /// <summary>
    /// Persiste o HTML gerado no diretÃ³rio de saÃ­da informado.
    /// </summary>
    public string Write(SolutionScanResult result, string outputDirectory)
    {
        var fileName = $"{result.SolutionName}-relatorio-migracao.html";
        var filePath = Path.Combine(outputDirectory, fileName);
        File.WriteAllText(filePath, Generate(result), Encoding.UTF8);
        return filePath;
    }

    /// <summary>
    /// ConstrÃ³i o HTML final com resumo, score de risco e seÃ§Ãµes de achados.
    /// </summary>
    public string Generate(SolutionScanResult result)
    {
        var encoder = HtmlEncoder.Default;
        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine($"  <title>Relatorio de Migracao: {encoder.Encode(result.SolutionName)}</title>");
        builder.AppendLine("  <meta charset=\"utf-8\">");
        builder.AppendLine("  <style>");
        builder.AppendLine("    body { font-family: 'Segoe UI', sans-serif; margin: 20px; color: #1b1b1b; }");
        builder.AppendLine("    .risk-score { font-size: 2.2em; font-weight: bold; margin: 12px 0; }");
        builder.AppendLine("    .blocker { border-left: 4px solid #d32f2f; padding-left: 12px; margin: 16px 0; }");
        builder.AppendLine("    .warning { border-left: 4px solid #f57c00; padding-left: 12px; margin: 16px 0; }");
        builder.AppendLine("    .info { border-left: 4px solid #388e3c; padding-left: 12px; margin: 16px 0; }");
        builder.AppendLine("    table { border-collapse: collapse; width: 100%; margin: 16px 0; }");
        builder.AppendLine("    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; vertical-align: top; }");
        builder.AppendLine("    th { background-color: #f2f2f2; }");
        builder.AppendLine("    .muted { color: #555; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>Relatorio de Compatibilidade de Migracao para .NET 10 LTS</h1>");
        builder.AppendLine($"  <p><strong>Solution:</strong> {encoder.Encode(Path.GetFileName(result.SolutionPath))}</p>");
        builder.AppendLine($"  <p><strong>Horario do Scan:</strong> {result.ScannedAt:yyyy-MM-dd HH:mm:ss}</p>");
        builder.AppendLine($"  <div class=\"risk-score\">Pontuacao de Risco: {result.Summary.RiskScore}/100</div>");
        builder.AppendLine("  <p class=\"muted\">Maior pontuacao indica mais bloqueadores exigindo atencao imediata antes da migracao para .NET 10.</p>");

        builder.AppendLine("  <h2>Bloqueadores Criticos</h2>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead><tr><th>Projeto</th><th>Problema</th><th>Impacto</th><th>Esforco</th><th>Sugestao de Correcao</th></tr></thead>");
        builder.AppendLine("    <tbody>");

        var blockerRows = BuildBlockerRows(result, encoder);
        if (blockerRows.Count == 0)
        {
            builder.AppendLine("      <tr><td colspan=\"5\">Nenhum bloqueador critico foi encontrado na execucao atual.</td></tr>");
        }
        else
        {
            foreach (var row in blockerRows)
            {
                builder.AppendLine(row);
            }
        }

        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");

        builder.AppendLine("  <h2>Resumo Executivo</h2>");
        builder.AppendLine("  <ul>");
        builder.AppendLine($"    <li>Projetos Escaneados: {result.Summary.ProjectsScanned}</li>");
        builder.AppendLine($"    <li>Bloqueadores Criticos: {result.Summary.CriticalBlockers}</li>");
        builder.AppendLine($"    <li>Avisos: {result.Summary.Warnings}</li>");
        builder.AppendLine($"    <li>Informacoes: {result.Summary.InformationalItems}</li>");
        builder.AppendLine("  </ul>");

        builder.AppendLine("  <h2>Projetos Avaliados</h2>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead><tr><th>Projeto</th><th>TFM Atual</th><th>Classificacao</th><th>Impacto Base</th><th>Resumo</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        foreach (var project in result.Projects.OrderBy(project => project.ProjectName, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"      <tr><td>{encoder.Encode(project.ProjectName)}</td><td>{encoder.Encode(string.Join(", ", project.TargetFrameworks))}</td><td>{encoder.Encode(project.MigrationProfile.Classification)}</td><td>{encoder.Encode(project.MigrationProfile.Impact)}</td><td>{encoder.Encode(project.MigrationProfile.Summary)}</td></tr>");
        }
        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");

        builder.AppendLine("  <h2>Avisos e Observacoes</h2>");
        foreach (var warning in result.PackageFindings.Where(finding => finding.IsWarning).OrderBy(finding => finding.ProjectName, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  <div class=\"warning\"><strong>{encoder.Encode(warning.ProjectName)} - {encoder.Encode(warning.PackageId)}</strong><br>{encoder.Encode(warning.Status)}<br>{encoder.Encode(warning.Recommendation)}<br><span class=\"muted\">{encoder.Encode(warning.Details)}</span></div>");
        }

        foreach (var info in result.PackageFindings.Where(finding => !finding.IsWarning && !finding.IsBlocker).OrderBy(finding => finding.ProjectName, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  <div class=\"info\"><strong>{encoder.Encode(info.ProjectName)} - {encoder.Encode(info.PackageId)}</strong><br>{encoder.Encode(info.Status)}<br><span class=\"muted\">{encoder.Encode(info.Details)}</span></div>");
        }

        builder.AppendLine($"  <p><em>Gerado pelo MigrationCompass {AppVersion} - Execute com --help para detalhes. Ultima atualizacao: Agosto/2026</em></p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    /// <summary>
    /// Consolida linhas de bloqueadores vindas de regras de API e de pacotes incompatÃ­veis.
    /// </summary>
    private static List<string> BuildBlockerRows(SolutionScanResult result, HtmlEncoder encoder)
    {
        var rows = new List<string>();

        foreach (var apiFinding in result.ApiFindings.Where(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase)))
        {
            rows.Add($"      <tr><td>{encoder.Encode(apiFinding.ProjectName)}</td><td>{encoder.Encode($"{apiFinding.Rule.Api} ({apiFinding.Rule.Id})")}</td><td>{encoder.Encode(apiFinding.Rule.Impact)}</td><td>{encoder.Encode(apiFinding.Rule.Effort)}</td><td>{encoder.Encode(apiFinding.Rule.Alternative)}</td></tr>");
        }

        foreach (var packageFinding in result.PackageFindings.Where(finding => finding.IsBlocker))
        {
            rows.Add($"      <tr><td>{encoder.Encode(packageFinding.ProjectName)}</td><td>{encoder.Encode($"{packageFinding.PackageId} ({packageFinding.RequestedVersion})")}</td><td>{encoder.Encode(packageFinding.Impact)}</td><td>Medio</td><td>{encoder.Encode(packageFinding.Recommendation)}</td></tr>");
        }

        return rows;
    }
}