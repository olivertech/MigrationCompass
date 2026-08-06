using System.Text;
using System.Text.Encodings.Web;
using MigrationCompass.Models;
using MigrationCompass.Services;

namespace MigrationCompass.Reporting;

/// <summary>
/// Gera o relatÃ³rio HTML autocontido com foco em bloqueadores que realmente movem decisÃ£o de migraÃ§Ã£o.
/// </summary>
public sealed class HtmlReportGenerator
{
    private const string AppVersion = "v3.2.0";

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
    /// ConstrÃ³i o HTML final com resumo executivo, bloqueadores crÃ­ticos, premissas econÃ´micas e visÃ£o curta por projeto.
    /// </summary>
    public string Generate(SolutionScanResult result)
    {
        var encoder = HtmlEncoder.Default;
        var economicParameters = result.EconomicParameters ?? CreateFallbackEconomicParameters();
        var costEstimator = new CostEstimator(economicParameters);
        var builder = new StringBuilder();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html>");
        builder.AppendLine("<head>");
        builder.AppendLine($"  <title>RelatÃ³rio de MigraÃ§Ã£o: {encoder.Encode(result.SolutionName)}</title>");
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
        builder.AppendLine("    .solid-multi-2-row { background: #fff8e1; }");
        builder.AppendLine("    .solid-multi-2-cell { border-left: 4px solid #f9a825; }");
        builder.AppendLine("    .solid-multi-3-row { background: #fff3e0; }");
        builder.AppendLine("    .solid-multi-3-cell { border-left: 4px solid #ef6c00; }");
        builder.AppendLine("    .solid-multi-4-row { background: #fff4f4; }");
        builder.AppendLine("    .solid-multi-4-cell { border-left: 4px solid #c62828; }");
        builder.AppendLine("    .solid-badge { display: inline-block; margin-top: 6px; padding: 3px 8px; border-radius: 999px; font-size: 0.82em; font-weight: 600; }");
        builder.AppendLine("    .solid-badge-multi-2 { background: #fff3cd; color: #8a5a00; border: 1px solid #f2c66d; }");
        builder.AppendLine("    .solid-badge-multi-3 { background: #ffe0b2; color: #a84300; border: 1px solid #ffb74d; }");
        builder.AppendLine("    .solid-badge-multi-4 { background: #fdecea; color: #b71c1c; border: 1px solid #ef9a9a; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("  <h1>RelatÃ³rio Executivo de MigraÃ§Ã£o para .NET 10</h1>");
        builder.AppendLine($"  <p><strong>Solution:</strong> {encoder.Encode(Path.GetFileName(result.SolutionPath))}</p>");
        builder.AppendLine($"  <p><strong>HorÃ¡rio do scan:</strong> {result.ScannedAt:yyyy-MM-dd HH:mm:ss}</p>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine($"    <div class=\"risk-score\">PontuaÃ§Ã£o de risco: {result.Summary.RiskScore}/100</div>");
        builder.AppendLine($"    <div class=\"risk-score\">PontuaÃ§Ã£o estrutural de manutenibilidade: {result.Summary.Maintainability.Score}/100</div>");
        builder.AppendLine("    <ul>");
        builder.AppendLine($"      <li>Projetos escaneados: {result.Summary.ProjectsScanned}</li>");
        builder.AppendLine($"      <li>Bloqueadores crÃ­ticos relevantes: {result.Summary.CriticalBlockers}</li>");
        builder.AppendLine($"      <li>Avisos tÃ©cnicos restantes: {result.Summary.Warnings}</li>");
        builder.AppendLine($"      <li>Itens informativos: {result.Summary.InformationalItems}</li>");
        builder.AppendLine($"      <li>ClassificaÃ§Ã£o de manutenibilidade: {encoder.Encode(result.Summary.Maintainability.Classification)}</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("    <p class=\"muted\">A pontuaÃ§Ã£o prioriza bloqueadores de runtime, dependÃªncias server-side e APIs legadas com impacto real na jornada para .NET 10.</p>");
        builder.AppendLine($"    <p class=\"muted\">{encoder.Encode(result.Summary.Maintainability.ExecutiveSummary)}</p>");
        builder.AppendLine("  </div>");

        builder.AppendLine("  <h2>PontuaÃ§Ã£o Estrutural de Manutenibilidade</h2>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine("    <p>Esta mÃ©trica combina quatro vetores: risco de migraÃ§Ã£o, densidade de sinais SOLID, idade tecnolÃ³gica e acoplamento a legado. O objetivo Ã© refletir o custo estrutural de manter e evoluir a solution, e nÃ£o apenas o esforÃ§o pontual de atualizaÃ§Ã£o.</p>");
        builder.AppendLine("  </div>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>Componente</th><th>Peso</th><th>Score Bruto</th><th>ContribuiÃ§Ã£o</th><th>Leitura</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        foreach (var component in EnumerateMaintainabilityComponents(result.Summary.Maintainability))
        {
            builder.AppendLine($"      <tr><td>{encoder.Encode(component.Name)}</td><td>{component.WeightPercent}%</td><td>{component.RawScore}/100</td><td>{component.WeightedScore} ponto(s)</td><td>{encoder.Encode(component.Explanation)}</td></tr>");
        }
        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");
        builder.AppendLine("  <div class=\"callout\">");
        builder.AppendLine("    <strong>Legenda gerencial da pontuaÃ§Ã£o:</strong>");
        builder.AppendLine("    <ul>");
        builder.AppendLine("      <li><strong>0 a 39 - ControlÃ¡vel:</strong> hÃ¡ espaÃ§o para evoluÃ§Ã£o incremental com menor pressÃ£o estrutural, embora ainda possam existir pontos localizados de atenÃ§Ã£o.</li>");
        builder.AppendLine("      <li><strong>40 a 64 - Moderada:</strong> a solution jÃ¡ apresenta sinais consistentes de desgaste tÃ©cnico e tende a exigir planejamento mais cuidadoso para sustentar novas entregas.</li>");
        builder.AppendLine("      <li><strong>65 a 84 - Alta:</strong> o custo de manter, adaptar e migrar cresce de forma perceptÃ­vel, com maior risco de retrabalho, acoplamento e baixa previsibilidade de execuÃ§Ã£o.</li>");
        builder.AppendLine("      <li><strong>85 a 100 - CrÃ­tica:</strong> o legado passa a indicar limitaÃ§Ã£o estrutural relevante, sugerindo avaliaÃ§Ã£o estratÃ©gica entre modernizaÃ§Ã£o profunda, transiÃ§Ã£o por etapas ou reconstruÃ§Ã£o parcial.</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("  </div>");

        if (result.Advisory is not null)
        {
            builder.AppendLine("  <h2>CenÃ¡rio Recomendado para Esta Solution</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p>{encoder.Encode(result.Advisory.ScenarioNarrative)}</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Leitura Gerencial</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p><strong>SÃ­ntese executiva:</strong> {encoder.Encode(result.Advisory.ExecutiveHeadline)}</p>");
            builder.AppendLine($"    <p><strong>Posicionamento recomendado:</strong> {encoder.Encode(result.Advisory.RecommendedStrategy)}</p>");
            builder.AppendLine($"    <p><strong>InterpretaÃ§Ã£o gerencial:</strong> {encoder.Encode(result.Advisory.ManagerialPositioning)}</p>");
            builder.AppendLine($"    <p><strong>Base tÃ©cnica da leitura:</strong> {encoder.Encode(result.Advisory.Rationale)}</p>");
            builder.AppendLine($"    <p><strong>DistÃ¢ncia tecnolÃ³gica observada:</strong> {encoder.Encode(result.Advisory.DistanceAssessment)}</p>");
            builder.AppendLine($"    <p><strong>Oportunidade nÃ£o capturada no cenÃ¡rio atual:</strong> {encoder.Encode(result.Advisory.OpportunitySummary)}</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Drivers da DecisÃ£o</h2>");
            builder.AppendLine("  <ul>");
            foreach (var driver in result.Advisory.DecisionDrivers)
            {
                builder.AppendLine($"    <li>{encoder.Encode(driver)}</li>");
            }

            builder.AppendLine("  </ul>");

            builder.AppendLine("  <h2>Caminhos EstratÃ©gicos PossÃ­veis</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Caminho</th><th>Quando faz sentido</th><th>EsforÃ§o</th><th>Risco Relativo</th><th>Leitura recomendada</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var path in result.Advisory.Paths)
            {
                var title = path.IsRecommended ? $"{path.Title} (Recomendado)" : path.Title;
                builder.AppendLine($"      <tr><td>{encoder.Encode(title)}</td><td>{encoder.Encode(path.Fit)}</td><td>{encoder.Encode(path.Effort)}</td><td>{encoder.Encode(path.IndicativeRisk)}</td><td>{encoder.Encode(path.Guidance)}</td></tr>");
            }

            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");
        }

        if (result.SolidFindings.Count > 0)
        {
            var solidOverviewRows = BuildSolidOverviewRows(result.SolidFindings);

            builder.AppendLine("  <h2>Qualidade de C\u00f3digo e Sinais de Ader\u00eancia ao SOLID</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p>Foram identificados {result.SolidFindings.Count} ind\u00edcio(s) heur\u00edstico(s) de poss\u00edvel fragilidade estrutural relacionada a princ\u00edpios SOLID. Esses achados n\u00e3o devem ser lidos como prova absoluta de viola\u00e7\u00e3o, mas como sinais \u00fateis de acoplamento, excesso de responsabilidade, contratos extensos ou abstra\u00e7\u00f5es fr\u00e1geis que podem elevar o custo de mudan\u00e7a em sistemas legados.</p>");
            builder.AppendLine("    <p>Um mesmo alvo pode concentrar sinais de mais de um princ\u00edpio ao mesmo tempo. Por isso, a leitura mais fiel deve considerar a combina\u00e7\u00e3o dos princ\u00edpios associados, e n\u00e3o apenas a primeira ocorr\u00eancia exibida.</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Legenda Executiva dos Princ\u00edpios SOLID</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princ\u00edpio</th><th>O que significa</th><th>Leitura gerencial</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var solidLegend in BuildSolidLegend())
            {
                builder.AppendLine($"      <tr><td>{encoder.Encode(solidLegend.Principle)}</td><td>{encoder.Encode(solidLegend.Meaning)}</td><td>{encoder.Encode(solidLegend.ManagerialReading)}</td></tr>");
            }

            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");

            builder.AppendLine("  <h2>Resumo por Princ\u00edpio</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princ\u00edpio</th><th>Ocorr\u00eancias</th><th>Alvos afetados</th><th>Leitura resumida</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var summary in BuildSolidPrincipleSummaries(result.SolidFindings))
            {
                builder.AppendLine($"      <tr><td>{encoder.Encode(summary.Principle)}</td><td>{summary.Findings}</td><td>{summary.Targets}</td><td>{encoder.Encode(summary.Reading)}</td></tr>");
            }

            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");

            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princ\u00edpios associados</th><th>Alvo</th><th>Severidade</th><th>Confian\u00e7a</th><th>Evid\u00eancia consolidada</th><th>Leitura consultiva</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var row in solidOverviewRows.Take(12))
            {
                var (rowClass, cellClass, badgeClass, badgeText) = GetSolidVisualLevel(row.PrincipleCount);
                var highlightClass = string.IsNullOrWhiteSpace(rowClass) ? string.Empty : $" class=\"{rowClass}\"";
                var principleCellClass = string.IsNullOrWhiteSpace(cellClass) ? string.Empty : $" class=\"{cellClass}\"";
                var badge = string.IsNullOrWhiteSpace(badgeText)
                    ? string.Empty
                    : $"<br><span class=\"solid-badge {badgeClass}\">{encoder.Encode(badgeText)}</span>";

                builder.AppendLine($"      <tr{highlightClass}><td{principleCellClass}>{encoder.Encode(row.Principles)}{badge}</td><td>{encoder.Encode(row.Target)}</td><td>{encoder.Encode(row.Severity)}</td><td>{encoder.Encode(row.Confidence)}</td><td>{encoder.Encode(row.Evidence)}</td><td>{encoder.Encode(row.Explanation)}</td></tr>");
            }

            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");

            builder.AppendLine("  <h2>Recomenda\u00e7\u00f5es de Refatora\u00e7\u00e3o Estrutural</h2>");
            builder.AppendLine("  <ul>");
            foreach (var recommendation in result.SolidFindings
                         .OrderByDescending(finding => SolidSeverityWeight(finding.Severity))
                         .Select(finding => finding.Recommendation)
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .Take(6))
            {
                builder.AppendLine($"    <li>{encoder.Encode(recommendation)}</li>");
            }

            builder.AppendLine("  </ul>");
        }

        builder.AppendLine("  <h2>Bloqueadores CrÃ­ticos (Impacto MensurÃ¡vel em ProduÃ§Ã£o)</h2>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead>");
        builder.AppendLine("      <tr><th>Bloqueador</th><th>Impacto de NegÃ³cio</th><th>EsforÃ§o para Mitigar</th><th>Custo Estimado de InaÃ§Ã£o (Mensal)</th></tr>");
        builder.AppendLine("    </thead>");
        builder.AppendLine("    <tbody>");

        var blockerRows = BuildCriticalRows(result, encoder, costEstimator);
        if (blockerRows.Count == 0)
        {
            builder.AppendLine("      <tr><td colspan=\"4\">Nenhum bloqueador crÃ­tico com impacto mensurÃ¡vel foi encontrado na execuÃ§Ã£o atual.</td></tr>");
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

        builder.AppendLine("  <h2>Premissas EconÃ´micas</h2>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>ParÃ¢metro</th><th>Faixa / Valor</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        builder.AppendLine($"      <tr><td>Custo hora estimado</td><td>{encoder.Encode(FormatCurrencyRange(economicParameters.HourlyRateMin, economicParameters.HourlyRateMax))}</td></tr>");
        builder.AppendLine($"      <tr><td>Semanas por mÃªs</td><td>{economicParameters.WeeksPerMonth:0.##}</td></tr>");
        builder.AppendLine($"      <tr><td>Banda baixa</td><td>{encoder.Encode(BuildBandSummary(economicParameters.Low))}</td></tr>");
        builder.AppendLine($"      <tr><td>Banda mÃ©dia</td><td>{encoder.Encode(BuildBandSummary(economicParameters.Medium))}</td></tr>");
        builder.AppendLine($"      <tr><td>Banda alta</td><td>{encoder.Encode(BuildBandSummary(economicParameters.High))}</td></tr>");
        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");

        builder.AppendLine("  <div class=\"callout\">");
        builder.AppendLine($"    <strong>Leitura recomendada:</strong> {encoder.Encode(economicParameters.Disclaimer ?? "Os valores do relatÃ³rio sÃ£o orientativos e devem ser usados como ponto de partida para aprofundamento tÃ©cnico e financeiro.")}");
        builder.AppendLine("  </div>");

        builder.AppendLine("  <h2>Panorama dos Projetos</h2>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>Projeto</th><th>TFM Atual</th><th>ClassificaÃ§Ã£o</th><th>Impacto Base</th><th>Resumo</th></tr></thead>");
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
            builder.AppendLine("    <strong>ObservaÃ§Ã£o:</strong> alguns pacotes nÃ£o puderam ser validados online. Recomenda-se uma nova execuÃ§Ã£o com acesso ao NuGet.org antes da decisÃ£o final.");
            builder.AppendLine("    <ul>");
            foreach (var warning in offlineWarnings.Take(6))
            {
                builder.AppendLine($"      <li>{encoder.Encode($"{warning.ProjectName} - {warning.PackageId}: {warning.Details}")}</li>");
            }

            if (offlineWarnings.Length > 6)
            {
                builder.AppendLine($"      <li>... e mais {offlineWarnings.Length - 6} pacote(s) nÃ£o verificado(s).</li>");
            }

            builder.AppendLine("    </ul>");
            builder.AppendLine("  </div>");
        }

        builder.AppendLine($"  <p><em>Gerado pelo MigrationCompass {AppVersion} em 2026-08-04.</em></p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    /// <summary>
    /// Consolida os principais bloqueadores em uma visÃ£o curta, pronta para apresentaÃ§Ã£o executiva.
    /// </summary>
    private static List<string> BuildCriticalRows(SolutionScanResult result, HtmlEncoder encoder, CostEstimator costEstimator)
    {
        var insights = new List<BlockerInsight>();

        foreach (var apiFinding in result.ApiFindings.Where(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase)))
        {
            insights.Add(new BlockerInsight(
                Priority: 100,
                Blocker: $"{apiFinding.Rule.Api} ({apiFinding.ProjectName})",
                BusinessImpact: apiFinding.Rule.BusinessImpact ?? BuildGenericApiImpact(apiFinding),
                Effort: apiFinding.Rule.Effort,
                MonthlyCost: CostEstimator.Format(costEstimator.Estimate(apiFinding.Rule))));
        }

        foreach (var packageFinding in result.PackageFindings.Where(finding => finding.IsBlocker))
        {
            insights.Add(new BlockerInsight(
                Priority: 90,
                Blocker: $"{packageFinding.PackageId} {packageFinding.RequestedVersion} ({packageFinding.ProjectName})",
                BusinessImpact: packageFinding.BusinessImpact ?? BuildGenericPackageImpact(packageFinding),
                Effort: packageFinding.Effort ?? "Medio",
                MonthlyCost: CostEstimator.Format(costEstimator.Estimate(packageFinding))));
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
        return $"{finding.Rule.Api} impede a adoÃ§Ã£o plena do pipeline moderno do ASP.NET Core, exigindo retrabalho arquitetural, ampliando janela de homologaÃ§Ã£o e elevando risco de indisponibilidade durante a migraÃ§Ã£o.";
    }

    private static string BuildGenericPackageImpact(PackageCompatibilityFinding finding)
    {
        return $"A dependÃªncia {finding.PackageId} nÃ£o possui trilha clara de compatibilidade com .NET 10. Isso tende a gerar atraso de cronograma, replanejamento tÃ©cnico e validaÃ§Ãµes extras antes de publicar a migraÃ§Ã£o em produÃ§Ã£o.";
    }

    private static string BuildBandSummary(EconomicBand band)
    {
        return $"horas/semana {band.WeeklyHoursMin:0.##}-{band.WeeklyHoursMax:0.##}, equipe {band.TeamSizeMin:0.##}-{band.TeamSizeMax:0.##}, infraestrutura {FormatCurrencyRange(band.InfraCostMin, band.InfraCostMax)}, risco {band.RiskMultiplierMin:0.##}-{band.RiskMultiplierMax:0.##}x";
    }

    private static string FormatCurrencyRange(decimal min, decimal max)
    {
        var range = new MonthlyCostRange
        {
            Min = min,
            Max = max
        };

        return CostEstimator.Format(range);
    }

    private static EconomicParameters CreateFallbackEconomicParameters()
    {
        return new EconomicParameters
        {
            HourlyRateMin = 140,
            HourlyRateMax = 240,
            WeeksPerMonth = 4.33m,
            Low = new EconomicBand
            {
                WeeklyHoursMin = 2,
                WeeklyHoursMax = 5,
                TeamSizeMin = 1,
                TeamSizeMax = 1.5m,
                InfraCostMin = 250,
                InfraCostMax = 900,
                RiskMultiplierMin = 1.00m,
                RiskMultiplierMax = 1.20m
            },
            Medium = new EconomicBand
            {
                WeeklyHoursMin = 4,
                WeeklyHoursMax = 10,
                TeamSizeMin = 1,
                TeamSizeMax = 2,
                InfraCostMin = 600,
                InfraCostMax = 2000,
                RiskMultiplierMin = 1.15m,
                RiskMultiplierMax = 1.45m
            },
            High = new EconomicBand
            {
                WeeklyHoursMin = 8,
                WeeklyHoursMax = 16,
                TeamSizeMin = 1.5m,
                TeamSizeMax = 3,
                InfraCostMin = 1500,
                InfraCostMax = 5000,
                RiskMultiplierMin = 1.35m,
                RiskMultiplierMax = 1.80m
            },
            Disclaimer = "Os valores do relatÃ³rio sÃ£o faixas orientativas construÃ­das a partir de premissas configurÃ¡veis de esforÃ§o tÃ©cnico, composiÃ§Ã£o de equipe e exposiÃ§Ã£o operacional. Eles servem como insumo inicial para priorizaÃ§Ã£o e aprofundamento do assessment, nÃ£o como estimativa financeira definitiva ou compromisso comercial."
        };
    }

    private static int SolidSeverityWeight(string severity)
    {
        return severity.Trim().ToLowerInvariant() switch
        {
            "alto" => 3,
            "mÃ©dio" => 2,
            "medio" => 2,
            _ => 1
        };
    }

    private static IEnumerable<MaintainabilityComponent> EnumerateMaintainabilityComponents(MaintainabilityAssessment assessment)
    {
        yield return assessment.MigrationRisk;
        yield return assessment.SolidDensity;
        yield return assessment.TechnologicalAge;
        yield return assessment.LegacyCoupling;
    }

    private static IReadOnlyList<SolidLegendItem> BuildSolidLegend()
    {
        return
        [
            new SolidLegendItem("SRP", "Single Responsibility Principle", "Ajuda a identificar classes ou m\u00e9todos que acumulam responsabilidades demais, elevando retrabalho, risco de defeitos e dificuldade de teste."),
            new SolidLegendItem("OCP", "Open/Closed Principle", "Aponta pontos em que a evolu\u00e7\u00e3o do sistema tende a exigir altera\u00e7\u00f5es recorrentes no mesmo c\u00f3digo, reduzindo previsibilidade de mudan\u00e7a."),
            new SolidLegendItem("LSP", "Liskov Substitution Principle", "Sinaliza heran\u00e7as ou substitui\u00e7\u00f5es potencialmente fr\u00e1geis, que podem quebrar comportamentos esperados e dificultar reutiliza\u00e7\u00e3o segura."),
            new SolidLegendItem("ISP", "Interface Segregation Principle", "Mostra contratos grandes demais, que for\u00e7am consumidores a depender de capacidades que talvez nem usem, aumentando acoplamento."),
            new SolidLegendItem("DIP", "Dependency Inversion Principle", "Evidencia acoplamento excessivo a implementa\u00e7\u00f5es concretas, o que costuma encarecer teste, troca de tecnologia e evolu\u00e7\u00e3o arquitetural.")
        ];
    }

    private static IEnumerable<SolidPrincipleSummary> BuildSolidPrincipleSummaries(IEnumerable<SolidFinding> findings)
    {
        foreach (var principle in OrderedPrinciples(findings.Select(finding => finding.Principle).Distinct(StringComparer.OrdinalIgnoreCase)))
        {
            var relatedFindings = findings
                .Where(finding => string.Equals(finding.Principle, principle, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            yield return new SolidPrincipleSummary(
                principle,
                relatedFindings.Length,
                relatedFindings
                    .Select(finding => $"{finding.ProjectName}|{finding.TargetName}|{finding.LineNumber}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(),
                BuildPrincipleReading(principle));
        }
    }

    private static IReadOnlyList<SolidOverviewRow> BuildSolidOverviewRows(IEnumerable<SolidFinding> findings)
    {
        return findings
            .GroupBy(finding => new { finding.ProjectName, finding.TargetName, finding.LineNumber, finding.FilePath })
            .Select(group =>
            {
                var groupedFindings = group.ToArray();
                var principles = OrderedPrinciples(groupedFindings.Select(finding => finding.Principle).Distinct(StringComparer.OrdinalIgnoreCase)).ToArray();
                var target = group.Key.LineNumber is null
                    ? $"{group.Key.TargetName} ({group.Key.ProjectName})"
                    : $"{group.Key.TargetName} ({group.Key.ProjectName}, linha {group.Key.LineNumber})";
                var highestSeverity = groupedFindings
                    .OrderByDescending(finding => SolidSeverityWeight(finding.Severity))
                    .ThenByDescending(finding => ConfidenceWeight(finding.Confidence))
                    .First();
                var evidence = string.Join(" | ", groupedFindings
                    .Select(finding => finding.Evidence)
                    .Where(evidence => !string.IsNullOrWhiteSpace(evidence))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3));
                var explanation = principles.Length > 1
                    ? $"O mesmo alvo concentra sinais simult\u00e2neos de {string.Join(", ", principles)}. Em termos gerenciais, isso sugere acoplamento estrutural combinado, baixa previsibilidade de mudan\u00e7a e maior risco de retrabalho."
                    : groupedFindings[0].Explanation;

                return new SolidOverviewRow(
                    string.Join(", ", principles),
                    target,
                    highestSeverity.Severity,
                    highestSeverity.Confidence,
                    evidence,
                    explanation,
                    principles.Length,
                    SolidSeverityWeight(highestSeverity.Severity));
            })
            .OrderByDescending(row => row.PrincipleCount)
            .ThenByDescending(row => row.SeverityWeight)
            .ThenBy(row => row.Target, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> OrderedPrinciples(IEnumerable<string> principles)
    {
        var order = new[] { "SRP", "OCP", "LSP", "ISP", "DIP" };
        return principles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(principle =>
            {
                var index = Array.FindIndex(order, candidate => string.Equals(candidate, principle, StringComparison.OrdinalIgnoreCase));
                return index < 0 ? int.MaxValue : index;
            })
            .ThenBy(principle => principle, StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildPrincipleReading(string principle)
    {
        return principle.ToUpperInvariant() switch
        {
            "SRP" => "Indica concentra\u00e7\u00e3o excessiva de responsabilidade em pontos isolados do sistema.",
            "OCP" => "Sugere baixa flexibilidade para evoluir sem alterar c\u00f3digo j\u00e1 estabilizado.",
            "LSP" => "Aponta risco de heran\u00e7as ou substitui\u00e7\u00f5es quebrarem comportamento esperado.",
            "ISP" => "Aponta contratos extensos, com consumo for\u00e7ado de capacidades desnecess\u00e1rias.",
            "DIP" => "Sinaliza depend\u00eancia excessiva de implementa\u00e7\u00f5es concretas e alto acoplamento.",
            _ => "Leitura heur\u00edstica de fragilidade estrutural."
        };
    }

    private static (string RowClass, string CellClass, string BadgeClass, string BadgeText) GetSolidVisualLevel(int principleCount)
    {
        return principleCount switch
        {
            >= 4 => ("solid-multi-4-row", "solid-multi-4-cell", "solid-badge-multi-4", "4+ princípios no mesmo alvo"),
            3 => ("solid-multi-3-row", "solid-multi-3-cell", "solid-badge-multi-3", "3 princípios no mesmo alvo"),
            2 => ("solid-multi-2-row", "solid-multi-2-cell", "solid-badge-multi-2", "2 princípios no mesmo alvo"),
            _ => (string.Empty, string.Empty, string.Empty, string.Empty)
        };
    }

    private static int ConfidenceWeight(string confidence)
    {
        return confidence.Trim().ToLowerInvariant() switch
        {
            "alta" => 3,
            "m\u00e9dia" => 2,
            "media" => 2,
            _ => 1
        };
    }

    private sealed record SolidLegendItem(string Principle, string Meaning, string ManagerialReading);
    private sealed record SolidPrincipleSummary(string Principle, int Findings, int Targets, string Reading);
    private sealed record SolidOverviewRow(string Principles, string Target, string Severity, string Confidence, string Evidence, string Explanation, int PrincipleCount, int SeverityWeight);
    private sealed record BlockerInsight(int Priority, string Blocker, string BusinessImpact, string Effort, string MonthlyCost);
}

