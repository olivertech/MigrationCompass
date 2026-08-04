using System.Text;
using System.Text.Encodings.Web;
using MigrationCompass.Models;

namespace MigrationCompass.Reporting;

/// <summary>
/// Gera o relatorio HTML autocontido com foco em bloqueadores que realmente movem decisao de migracao.
/// </summary>
public sealed class HtmlReportGenerator
{
    private const string AppVersion = "v3.1.0";

    /// <summary>
    /// Persiste o HTML gerado no diretorio de saida informado.
    /// </summary>
    public string Write(SolutionScanResult result, string outputDirectory)
    {
        var fileName = $"{result.SolutionName}-relatorio-migracao.html";
        var filePath = Path.Combine(outputDirectory, fileName);
        File.WriteAllText(filePath, Generate(result), Encoding.UTF8);
        return filePath;
    }

    /// <summary>
    /// Constroi o HTML final com resumo executivo, bloqueadores criticos e visao curta por projeto.
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
        builder.AppendLine("    h1, h2 { color: #16213e; }");
        builder.AppendLine("    .risk-score { font-size: 2.2em; font-weight: bold; margin: 12px 0; color: #b71c1c; }");
        builder.AppendLine("    .muted { color: #555; }");
        builder.AppendLine("    .summary-card { background: #f7f9fc; border: 1px solid #d9e2f2; padding: 16px; margin: 16px 0; border-radius: 8px; }");
        builder.AppendLine("    .callout { background: #fff8e1; border-left: 4px solid #f9a825; padding: 12px; margin: 16px 0; }");
        builder.AppendLine("    table { border-collapse: collapse; width: 100%; margin: 16px 0; }");
        builder.AppendLine("    th, td { border: 1px solid #ddd; padding: 10px; text-align: left; vertical-align: top; }");
        builder.AppendLine("    th { background-color: #f2f2f2; }");
        builder.AppendLine("    .compact td, .compact th { padding: 8px; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>Relatorio Executivo de Migracao para .NET 10</h1>");
        builder.AppendLine($"  <p><strong>Solution:</strong> {encoder.Encode(Path.GetFileName(result.SolutionPath))}</p>");
        builder.AppendLine($"  <p><strong>Horario do scan:</strong> {result.ScannedAt:yyyy-MM-dd HH:mm:ss}</p>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine($"    <div class=\"risk-score\">Pontuacao de risco: {result.Summary.RiskScore}/100</div>");
        builder.AppendLine("    <ul>");
        builder.AppendLine($"      <li>Projetos escaneados: {result.Summary.ProjectsScanned}</li>");
        builder.AppendLine($"      <li>Bloqueadores criticos relevantes: {result.Summary.CriticalBlockers}</li>");
        builder.AppendLine($"      <li>Avisos tecnicos restantes: {result.Summary.Warnings}</li>");
        builder.AppendLine($"      <li>Itens informativos: {result.Summary.InformationalItems}</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("    <p class=\"muted\">A pontuacao prioriza bloqueadores de runtime, dependencias server-side e APIs legadas com impacto real na jornada para .NET 10.</p>");
        builder.AppendLine("  </div>");

        builder.AppendLine("  <h2>🚨 Bloqueadores Criticos (Impacto Mensuravel em Producao)</h2>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead>");
        builder.AppendLine("      <tr><th>Bloqueador</th><th>Impacto de Negocio</th><th>Esforco para Mitigar</th><th>Custo Estimado de Inacao (Mensal)</th></tr>");
        builder.AppendLine("    </thead>");
        builder.AppendLine("    <tbody>");

        var blockerRows = BuildCriticalRows(result, encoder);
        if (blockerRows.Count == 0)
        {
            builder.AppendLine("      <tr><td colspan=\"4\">Nenhum bloqueador critico com impacto mensuravel foi encontrado na execucao atual.</td></tr>");
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

        builder.AppendLine("  <h2>Panorama dos Projetos</h2>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>Projeto</th><th>TFM Atual</th><th>Classificacao</th><th>Impacto Base</th><th>Resumo</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        foreach (var project in result.Projects.OrderBy(project => project.ProjectName, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"      <tr><td>{encoder.Encode(project.ProjectName)}</td><td>{encoder.Encode(string.Join(", ", project.TargetFrameworks))}</td><td>{encoder.Encode(project.MigrationProfile.Classification)}</td><td>{encoder.Encode(project.MigrationProfile.Impact)}</td><td>{encoder.Encode(project.MigrationProfile.Summary)}</td></tr>");
        }
        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");

        var offlineWarnings = result.PackageFindings
            .Where(finding => string.Equals(finding.Status, "Nao verificado offline", StringComparison.OrdinalIgnoreCase))
            .OrderBy(finding => finding.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.PackageId, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (offlineWarnings.Length > 0)
        {
            builder.AppendLine("  <div class=\"callout\">");
            builder.AppendLine("    <strong>Observacao:</strong> alguns pacotes nao puderam ser validados online. Recomenda-se uma nova execucao com acesso ao NuGet.org antes da decisao final.");
            builder.AppendLine("    <ul>");
            foreach (var warning in offlineWarnings.Take(6))
            {
                builder.AppendLine($"      <li>{encoder.Encode($"{warning.ProjectName} - {warning.PackageId}: {warning.Details}")}</li>");
            }

            if (offlineWarnings.Length > 6)
            {
                builder.AppendLine($"      <li>... e mais {offlineWarnings.Length - 6} pacote(s) nao verificado(s).</li>");
            }

            builder.AppendLine("    </ul>");
            builder.AppendLine("  </div>");
        }

        builder.AppendLine($"  <p><em>Gerado pelo MigrationCompass {AppVersion} em 2026-08-03.</em></p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    /// <summary>
    /// Consolida os principais bloqueadores em uma visao curta, pronta para apresentacao executiva.
    /// </summary>
    private static List<string> BuildCriticalRows(SolutionScanResult result, HtmlEncoder encoder)
    {
        var insights = new List<BlockerInsight>();

        foreach (var apiFinding in result.ApiFindings.Where(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase)))
        {
            insights.Add(new BlockerInsight(
                Priority: 100,
                Blocker: $"{apiFinding.Rule.Api} ({apiFinding.ProjectName})",
                BusinessImpact: apiFinding.Rule.BusinessImpact ?? BuildGenericApiImpact(apiFinding),
                Effort: apiFinding.Rule.Effort,
                MonthlyCost: apiFinding.Rule.MonthlyInactionCost ?? "A estimar apos baseline de infraestrutura"));
        }

        foreach (var packageFinding in result.PackageFindings.Where(finding => finding.IsBlocker))
        {
            insights.Add(new BlockerInsight(
                Priority: 90,
                Blocker: $"{packageFinding.PackageId} {packageFinding.RequestedVersion} ({packageFinding.ProjectName})",
                BusinessImpact: packageFinding.BusinessImpact ?? BuildGenericPackageImpact(packageFinding),
                Effort: packageFinding.Effort ?? "Medio",
                MonthlyCost: packageFinding.EstimatedMonthlyInactionCost ?? "A estimar apos discovery tecnico"));
        }

        return insights
            .GroupBy(item => item.Blocker, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.Blocker, StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(item => $"      <tr><td>{encoder.Encode(item.Blocker)}</td><td>{encoder.Encode(item.BusinessImpact)}</td><td>{encoder.Encode(item.Effort)}</td><td>{encoder.Encode(item.MonthlyCost)}</td></tr>")
            .ToList();
    }

    private static string BuildGenericApiImpact(ApiFinding finding)
    {
        return $"{finding.Rule.Api} impede a adocao plena do pipeline moderno do ASP.NET Core, exigindo retrabalho arquitetural, ampliando janela de homologacao e elevando risco de indisponibilidade durante a migracao.";
    }

    private static string BuildGenericPackageImpact(PackageCompatibilityFinding finding)
    {
        return $"A dependencia {finding.PackageId} nao possui trilha clara de compatibilidade com .NET 10. Isso tende a gerar atraso de cronograma, replanejamento tecnico e validacoes extras antes de publicar a migracao em producao.";
    }

    private sealed record BlockerInsight(int Priority, string Blocker, string BusinessImpact, string Effort, string MonthlyCost);
}
