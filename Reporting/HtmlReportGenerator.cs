using System.Text;
using System.Text.Encodings.Web;
using MigrationCompass.Models;
using MigrationCompass.Services;

namespace MigrationCompass.Reporting;

/// <summary>
/// Gera o relatório HTML autocontido com foco em bloqueadores que realmente movem decisão de migração.
/// </summary>
public sealed class HtmlReportGenerator
{
    private const string AppVersion = "v3.2.0";

    /// <summary>
    /// Persiste o HTML gerado no diretório de saída informado.
    /// </summary>
    public string Write(SolutionScanResult result, string outputDirectory)
    {
        var fileName = $"{result.SolutionName}-relatorio-migracao.html";
        var filePath = Path.Combine(outputDirectory, fileName);
        File.WriteAllText(filePath, Generate(result), Encoding.UTF8);
        return filePath;
    }

    /// <summary>
    /// Constrói o HTML final com resumo executivo, bloqueadores críticos, premissas econômicas e visão curta por projeto.
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
        builder.AppendLine($"  <title>Relatório de Migração: {encoder.Encode(result.SolutionName)}</title>");
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
        builder.AppendLine("  <h1>Relatório Executivo de Migração para .NET 10</h1>");
        builder.AppendLine($"  <p><strong>Solution:</strong> {encoder.Encode(Path.GetFileName(result.SolutionPath))}</p>");
        builder.AppendLine($"  <p><strong>Horário do scan:</strong> {result.ScannedAt:yyyy-MM-dd HH:mm:ss}</p>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine($"    <div class=\"risk-score\">Pontuação de risco: {result.Summary.RiskScore}/100</div>");
        builder.AppendLine($"    <div class=\"risk-score\">Pontuação estrutural de manutenibilidade: {result.Summary.Maintainability.Score}/100</div>");
        builder.AppendLine("    <ul>");
        builder.AppendLine($"      <li>Projetos escaneados: {result.Summary.ProjectsScanned}</li>");
        builder.AppendLine($"      <li>Bloqueadores críticos relevantes: {result.Summary.CriticalBlockers}</li>");
        builder.AppendLine($"      <li>Avisos técnicos restantes: {result.Summary.Warnings}</li>");
        builder.AppendLine($"      <li>Itens informativos: {result.Summary.InformationalItems}</li>");
        builder.AppendLine($"      <li>Classificação de manutenibilidade: {encoder.Encode(result.Summary.Maintainability.Classification)}</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("    <p class=\"muted\">A pontuação prioriza bloqueadores de runtime, dependências server-side e APIs legadas com impacto real na jornada para .NET 10.</p>");
        builder.AppendLine($"    <p class=\"muted\">{encoder.Encode(result.Summary.Maintainability.ExecutiveSummary)}</p>");
        builder.AppendLine("  </div>");

        builder.AppendLine("  <h2>Pontuação Estrutural de Manutenibilidade</h2>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine("    <p>Esta métrica combina quatro vetores: risco de migração, densidade de sinais SOLID, idade tecnológica e acoplamento a legado. O objetivo é refletir o custo estrutural de manter e evoluir a solution, e não apenas o esforço pontual de atualização.</p>");
        builder.AppendLine("  </div>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>Componente</th><th>Peso</th><th>Score Bruto</th><th>Contribuição</th><th>Leitura</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        foreach (var component in EnumerateMaintainabilityComponents(result.Summary.Maintainability))
        {
            builder.AppendLine($"      <tr><td>{encoder.Encode(component.Name)}</td><td>{component.WeightPercent}%</td><td>{component.RawScore}/100</td><td>{component.WeightedScore} ponto(s)</td><td>{encoder.Encode(component.Explanation)}</td></tr>");
        }
        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");
        builder.AppendLine("  <div class=\"callout\">");
        builder.AppendLine("    <strong>Legenda gerencial da pontuação:</strong>");
        builder.AppendLine("    <ul>");
        builder.AppendLine("      <li><strong>0 a 39 - Controlável:</strong> há espaço para evolução incremental com menor pressão estrutural, embora ainda possam existir pontos localizados de atenção.</li>");
        builder.AppendLine("      <li><strong>40 a 64 - Moderada:</strong> a solution já apresenta sinais consistentes de desgaste técnico e tende a exigir planejamento mais cuidadoso para sustentar novas entregas.</li>");
        builder.AppendLine("      <li><strong>65 a 84 - Alta:</strong> o custo de manter, adaptar e migrar cresce de forma perceptível, com maior risco de retrabalho, acoplamento e baixa previsibilidade de execução.</li>");
        builder.AppendLine("      <li><strong>85 a 100 - Crítica:</strong> o legado passa a indicar limitação estrutural relevante, sugerindo avaliação estratégica entre modernização profunda, transição por etapas ou reconstrução parcial.</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("  </div>");

        if (result.Advisory is not null)
        {
            builder.AppendLine("  <h2>Cenário Recomendado para Esta Solution</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p>{encoder.Encode(result.Advisory.ScenarioNarrative)}</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Leitura Gerencial</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p><strong>Síntese executiva:</strong> {encoder.Encode(result.Advisory.ExecutiveHeadline)}</p>");
            builder.AppendLine($"    <p><strong>Posicionamento recomendado:</strong> {encoder.Encode(result.Advisory.RecommendedStrategy)}</p>");
            builder.AppendLine($"    <p><strong>Interpretação gerencial:</strong> {encoder.Encode(result.Advisory.ManagerialPositioning)}</p>");
            builder.AppendLine($"    <p><strong>Base técnica da leitura:</strong> {encoder.Encode(result.Advisory.Rationale)}</p>");
            builder.AppendLine($"    <p><strong>Distância tecnológica observada:</strong> {encoder.Encode(result.Advisory.DistanceAssessment)}</p>");
            builder.AppendLine($"    <p><strong>Oportunidade não capturada no cenário atual:</strong> {encoder.Encode(result.Advisory.OpportunitySummary)}</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Drivers da Decisão</h2>");
            builder.AppendLine("  <ul>");
            foreach (var driver in result.Advisory.DecisionDrivers)
            {
                builder.AppendLine($"    <li>{encoder.Encode(driver)}</li>");
            }

            builder.AppendLine("  </ul>");

            builder.AppendLine("  <h2>Caminhos Estratégicos Possíveis</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Caminho</th><th>Quando faz sentido</th><th>Esforço</th><th>Risco Relativo</th><th>Leitura recomendada</th></tr></thead>");
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
            builder.AppendLine("  <h2>Qualidade de Código e Sinais de Aderência ao SOLID</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p>Foram identificados {result.SolidFindings.Count} indício(s) heurístico(s) de possível fragilidade estrutural relacionada a princípios SOLID. Esses achados não devem ser lidos como prova absoluta de violação, mas como sinais úteis de acoplamento, excesso de responsabilidade, contratos extensos ou abstrações frágeis que podem elevar o custo de mudança em sistemas legados.</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princípio</th><th>Alvo</th><th>Severidade</th><th>Confiança</th><th>Evidência</th><th>Leitura consultiva</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var finding in result.SolidFindings.Take(10))
            {
                var target = finding.LineNumber is null
                    ? $"{finding.TargetName} ({finding.ProjectName})"
                    : $"{finding.TargetName} ({finding.ProjectName}, linha {finding.LineNumber})";

                builder.AppendLine($"      <tr><td>{encoder.Encode(finding.Principle)}</td><td>{encoder.Encode(target)}</td><td>{encoder.Encode(finding.Severity)}</td><td>{encoder.Encode(finding.Confidence)}</td><td>{encoder.Encode(finding.Evidence)}</td><td>{encoder.Encode(finding.Explanation)}</td></tr>");
            }

            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");

            builder.AppendLine("  <h2>Recomendações de Refatoração Estrutural</h2>");
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

        builder.AppendLine("  <h2>Bloqueadores Críticos (Impacto Mensurável em Produção)</h2>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead>");
        builder.AppendLine("      <tr><th>Bloqueador</th><th>Impacto de Negócio</th><th>Esforço para Mitigar</th><th>Custo Estimado de Inação (Mensal)</th></tr>");
        builder.AppendLine("    </thead>");
        builder.AppendLine("    <tbody>");

        var blockerRows = BuildCriticalRows(result, encoder, costEstimator);
        if (blockerRows.Count == 0)
        {
            builder.AppendLine("      <tr><td colspan=\"4\">Nenhum bloqueador crítico com impacto mensurável foi encontrado na execução atual.</td></tr>");
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

        builder.AppendLine("  <h2>Premissas Econômicas</h2>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>Parâmetro</th><th>Faixa / Valor</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        builder.AppendLine($"      <tr><td>Custo hora estimado</td><td>{encoder.Encode(FormatCurrencyRange(economicParameters.HourlyRateMin, economicParameters.HourlyRateMax))}</td></tr>");
        builder.AppendLine($"      <tr><td>Semanas por mês</td><td>{economicParameters.WeeksPerMonth:0.##}</td></tr>");
        builder.AppendLine($"      <tr><td>Banda baixa</td><td>{encoder.Encode(BuildBandSummary(economicParameters.Low))}</td></tr>");
        builder.AppendLine($"      <tr><td>Banda média</td><td>{encoder.Encode(BuildBandSummary(economicParameters.Medium))}</td></tr>");
        builder.AppendLine($"      <tr><td>Banda alta</td><td>{encoder.Encode(BuildBandSummary(economicParameters.High))}</td></tr>");
        builder.AppendLine("    </tbody>");
        builder.AppendLine("  </table>");

        builder.AppendLine("  <div class=\"callout\">");
        builder.AppendLine($"    <strong>Leitura recomendada:</strong> {encoder.Encode(economicParameters.Disclaimer ?? "Os valores do relatório são orientativos e devem ser usados como ponto de partida para aprofundamento técnico e financeiro.")}");
        builder.AppendLine("  </div>");

        builder.AppendLine("  <h2>Panorama dos Projetos</h2>");
        builder.AppendLine("  <table class=\"compact\">");
        builder.AppendLine("    <thead><tr><th>Projeto</th><th>TFM Atual</th><th>Classificação</th><th>Impacto Base</th><th>Resumo</th></tr></thead>");
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
            builder.AppendLine("    <strong>Observação:</strong> alguns pacotes não puderam ser validados online. Recomenda-se uma nova execução com acesso ao NuGet.org antes da decisão final.");
            builder.AppendLine("    <ul>");
            foreach (var warning in offlineWarnings.Take(6))
            {
                builder.AppendLine($"      <li>{encoder.Encode($"{warning.ProjectName} - {warning.PackageId}: {warning.Details}")}</li>");
            }

            if (offlineWarnings.Length > 6)
            {
                builder.AppendLine($"      <li>... e mais {offlineWarnings.Length - 6} pacote(s) não verificado(s).</li>");
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
    /// Consolida os principais bloqueadores em uma visão curta, pronta para apresentação executiva.
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
        return $"{finding.Rule.Api} impede a adoção plena do pipeline moderno do ASP.NET Core, exigindo retrabalho arquitetural, ampliando janela de homologação e elevando risco de indisponibilidade durante a migração.";
    }

    private static string BuildGenericPackageImpact(PackageCompatibilityFinding finding)
    {
        return $"A dependência {finding.PackageId} não possui trilha clara de compatibilidade com .NET 10. Isso tende a gerar atraso de cronograma, replanejamento técnico e validações extras antes de publicar a migração em produção.";
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
            Disclaimer = "Os valores do relatório são faixas orientativas construídas a partir de premissas configuráveis de esforço técnico, composição de equipe e exposição operacional. Eles servem como insumo inicial para priorização e aprofundamento do assessment, não como estimativa financeira definitiva ou compromisso comercial."
        };
    }

    private static int SolidSeverityWeight(string severity)
    {
        return severity.Trim().ToLowerInvariant() switch
        {
            "alto" => 3,
            "médio" => 2,
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

    private sealed record BlockerInsight(int Priority, string Blocker, string BusinessImpact, string Effort, string MonthlyCost);
}
