using System.Text;
using System.Text.Encodings.Web;
using MigrationCompass.Models;
using MigrationCompass.Services;

namespace MigrationCompass.Reporting;

/// <summary>
/// Gera o relatório HTML autocontido com foco em leitura executiva e credibilidade analítica.
/// </summary>
public sealed class HtmlReportGenerator
{
    private const string AppVersion = "v3.3.0";

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
    /// Constrói o HTML final com foco em risco, fragilidade estrutural e leitura gerencial.
    /// </summary>
    public string Generate(SolutionScanResult result)
    {
        var encoder = HtmlEncoder.Default;
        var economicParameters = result.EconomicParameters ?? CreateFallbackEconomicParameters();
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
        builder.AppendLine("    .fragility-score { font-size: 2.2em; font-weight: bold; margin: 12px 0; color: #7b1fa2; }");
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
        builder.AppendLine("  <h1>Relatório Executivo de Migração para .NET 10</h1>");
        builder.AppendLine($"  <p><strong>Solution:</strong> {encoder.Encode(Path.GetFileName(result.SolutionPath))}</p>");
        builder.AppendLine($"  <p><strong>Horário do scan:</strong> {result.ScannedAt:yyyy-MM-dd HH:mm:ss}</p>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine($"    <div class=\"risk-score\">Pontuação de risco: {result.Summary.RiskScore}/100</div>");
        builder.AppendLine($"    <div class=\"fragility-score\">Índice de fragilidade estrutural: {result.Summary.Maintainability.Score}/100</div>");
        builder.AppendLine("    <ul>");
        builder.AppendLine($"      <li>Projetos escaneados: {result.Summary.ProjectsScanned}</li>");
        builder.AppendLine($"      <li>Bloqueadores críticos relevantes: {result.Summary.CriticalBlockers}</li>");
        builder.AppendLine($"      <li>Avisos técnicos restantes: {result.Summary.Warnings}</li>");
        builder.AppendLine($"      <li>Itens informativos: {result.Summary.InformationalItems}</li>");
        builder.AppendLine($"      <li>Classificação de fragilidade estrutural: {encoder.Encode(result.Summary.Maintainability.Classification)}</li>");
        builder.AppendLine($"      <li>Artefatos gerados ou de scaffolding isolados da análise estrutural: {result.GeneratedArtifacts.Count}</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("    <p class=\"muted\">A pontuação de risco prioriza bloqueadores distintos, diversidade de frentes críticas e contexto da base, em vez de apenas saturar com repetição do mesmo padrão.</p>");
        builder.AppendLine($"    <p class=\"muted\">{encoder.Encode(result.Summary.Maintainability.ExecutiveSummary)}</p>");
        builder.AppendLine("  </div>");

        builder.AppendLine("  <h2>Índice de Fragilidade Estrutural</h2>");
        builder.AppendLine("  <div class=\"summary-card\">");
        builder.AppendLine("    <p>Esta métrica representa fragilidade e risco estrutural da base, e não boa manutenibilidade. Quanto maior o índice, maior tende a ser o custo de manter, adaptar e evoluir a solution.</p>");
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
        builder.AppendLine("    <strong>Legenda gerencial da fragilidade estrutural:</strong>");
        builder.AppendLine("    <ul>");
        builder.AppendLine("      <li><strong>0 a 39 - Controlável:</strong> há espaço para evolução incremental com menor pressão estrutural.</li>");
        builder.AppendLine("      <li><strong>40 a 64 - Moderada:</strong> a base já apresenta desgaste técnico consistente e exige planejamento cuidadoso.</li>");
        builder.AppendLine("      <li><strong>65 a 84 - Alta:</strong> o custo de manter, adaptar e migrar cresce de forma perceptível, com risco relevante de retrabalho.</li>");
        builder.AppendLine("      <li><strong>85 a 100 - Crítica:</strong> a base sugere limitação estrutural importante e pede decisão estratégica mais cuidadosa.</li>");
        builder.AppendLine("    </ul>");
        builder.AppendLine("  </div>");

        if (result.Advisory is not null)
        {
            builder.AppendLine("  <h2>Cenário com Maior Aderência aos Sinais Encontrados</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p>{encoder.Encode(result.Advisory.ScenarioNarrative)}</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Leitura Gerencial</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p><strong>Síntese executiva:</strong> {encoder.Encode(result.Advisory.ExecutiveHeadline)}</p>");
            builder.AppendLine($"    <p><strong>Cenário com maior aderência aos sinais encontrados:</strong> {encoder.Encode(result.Advisory.RecommendedStrategy)}</p>");
            builder.AppendLine($"    <p><strong>Interpretação gerencial:</strong> {encoder.Encode(result.Advisory.ManagerialPositioning)}</p>");
            builder.AppendLine($"    <p><strong>Base técnica da leitura:</strong> {encoder.Encode(result.Advisory.Rationale)}</p>");
            builder.AppendLine($"    <p><strong>Distância tecnológica observada:</strong> {encoder.Encode(result.Advisory.DistanceAssessment)}</p>");
            builder.AppendLine($"    <p><strong>Oportunidade não capturada no cenário atual:</strong> {encoder.Encode(result.Advisory.OpportunitySummary)}</p>");
            builder.AppendLine("  </div>");
            builder.AppendLine("  <div class=\"callout\">");
            builder.AppendLine("    <strong>Importante:</strong> Esta leitura é orientativa e depende de discovery técnico e de negócio para validação executiva.");
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
                var title = path.IsRecommended ? $"{path.Title} (Maior aderência)" : path.Title;
                builder.AppendLine($"      <tr><td>{encoder.Encode(title)}</td><td>{encoder.Encode(path.Fit)}</td><td>{encoder.Encode(path.Effort)}</td><td>{encoder.Encode(path.IndicativeRisk)}</td><td>{encoder.Encode(path.Guidance)}</td></tr>");
            }
            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");
        }

        if (result.SolidFindings.Count > 0)
        {
            var solidOverviewRows = BuildSolidOverviewRows(result.SolidFindings);

            builder.AppendLine("  <h2>Sinais estruturais que merecem revisão</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine($"    <p>Foram identificados {result.SolidFindings.Count} indício(s) heurístico(s) de possível fragilidade estrutural. Eles não devem ser lidos como violação comprovada, mas como sinais úteis de acoplamento, concentração de responsabilidade, contratos extensos ou abstrações frágeis que merecem revisão.</p>");
            builder.AppendLine("    <p>A taxonomia SOLID é usada aqui como apoio de leitura para organizar os indícios, não como diagnóstico conclusivo isolado.</p>");
            builder.AppendLine("  </div>");

            builder.AppendLine("  <h2>Legenda Executiva dos Princípios SOLID</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princípio</th><th>O que significa</th><th>Leitura gerencial</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var solidLegend in BuildSolidLegend())
            {
                builder.AppendLine($"      <tr><td>{encoder.Encode(solidLegend.Principle)}</td><td>{encoder.Encode(solidLegend.Meaning)}</td><td>{encoder.Encode(solidLegend.ManagerialReading)}</td></tr>");
            }
            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");

            builder.AppendLine("  <h2>Resumo por Princípio</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princípio</th><th>Ocorrências</th><th>Alvos afetados</th><th>Leitura resumida</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var summary in BuildSolidPrincipleSummaries(result.SolidFindings))
            {
                builder.AppendLine($"      <tr><td>{encoder.Encode(summary.Principle)}</td><td>{summary.Findings}</td><td>{summary.Targets}</td><td>{encoder.Encode(summary.Reading)}</td></tr>");
            }
            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");

            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Princípios associados</th><th>Alvo</th><th>Severidade</th><th>Confiança</th><th>Evidência consolidada</th><th>Leitura consultiva</th></tr></thead>");
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

            builder.AppendLine("  <h2>Recomendações de Revisão Estrutural</h2>");
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

        builder.AppendLine("  <h2>Bloqueadores Críticos Relevantes</h2>");
        builder.AppendLine("  <table>");
        builder.AppendLine("    <thead><tr><th>Bloqueador</th><th>Impacto de negócio</th><th>Esforço para mitigar</th></tr></thead>");
        builder.AppendLine("    <tbody>");
        var blockerRows = BuildCriticalRows(result, encoder);
        if (blockerRows.Count == 0)
        {
            builder.AppendLine("      <tr><td colspan=\"3\">Nenhum bloqueador crítico relevante foi encontrado na execução atual.</td></tr>");
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

        if (result.EconomicExposureScenarios.Count > 0)
        {
            builder.AppendLine("  <h2>Exposição Econômica Orientativa por Cenário</h2>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Cenário</th><th>Leitura</th><th>Sinais</th><th>Faixa orientativa</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var scenario in result.EconomicExposureScenarios)
            {
                builder.AppendLine($"      <tr><td>{encoder.Encode(scenario.Title)}</td><td>{encoder.Encode(scenario.Summary)}</td><td>{scenario.Signals}</td><td>{encoder.Encode(CostEstimator.Format(scenario.Range))}</td></tr>");
            }
            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");
            builder.AppendLine("  <div class=\"callout\">");
            builder.AppendLine("    <strong>Leitura recomendada:</strong> esta exposição econômica é orientativa, não aditiva entre cenários e não deve ser lida como orçamento ou soma direta por bloqueador individual.");
            builder.AppendLine("  </div>");
        }

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
        builder.AppendLine($"    <strong>Premissa de uso:</strong> {encoder.Encode(economicParameters.Disclaimer ?? "Os valores do relatório são orientativos e devem ser usados como ponto de partida para aprofundamento técnico e financeiro.")}");
        builder.AppendLine("  </div>");

        if (result.GeneratedArtifacts.Count > 0)
        {
            builder.AppendLine("  <h2>Artefatos gerados ou de scaffolding identificados</h2>");
            builder.AppendLine("  <div class=\"summary-card\">");
            builder.AppendLine("    <p>Os itens abaixo foram isolados da análise estrutural principal para reduzir ruído analítico. Eles podem exigir revisão contextual, mas não entram no score estrutural desta execução.</p>");
            builder.AppendLine("  </div>");
            builder.AppendLine("  <table class=\"compact\">");
            builder.AppendLine("    <thead><tr><th>Projeto</th><th>Arquivo</th><th>Categoria</th><th>Motivo</th></tr></thead>");
            builder.AppendLine("    <tbody>");
            foreach (var artifact in result.GeneratedArtifacts
                         .OrderBy(item => item.ProjectName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.FilePath, StringComparer.OrdinalIgnoreCase)
                         .Take(20))
            {
                builder.AppendLine($"      <tr><td>{encoder.Encode(artifact.ProjectName)}</td><td>{encoder.Encode(Path.GetFileName(artifact.FilePath))}</td><td>{encoder.Encode(artifact.Category)}</td><td>{encoder.Encode(artifact.Reason)}</td></tr>");
            }
            builder.AppendLine("    </tbody>");
            builder.AppendLine("  </table>");
        }

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
            builder.AppendLine("    <strong>Observação:</strong> alguns pacotes não puderam ser validados online. Recomenda-se uma nova execução com acesso ao NuGet.org antes de qualquer decisão final.");
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

        builder.AppendLine($"  <p><em>Gerado pelo MigrationCompass {AppVersion} em {DateTime.Now:yyyy-MM-dd}.</em></p>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static List<string> BuildCriticalRows(SolutionScanResult result, HtmlEncoder encoder)
    {
        var insights = new List<BlockerInsight>();

        foreach (var apiFinding in result.ApiFindings.Where(finding => string.Equals(finding.Rule.Impact, "Alto", StringComparison.OrdinalIgnoreCase)))
        {
            insights.Add(new BlockerInsight(
                Priority: 100,
                Blocker: $"{apiFinding.Rule.Api} ({apiFinding.ProjectName})",
                BusinessImpact: apiFinding.Rule.BusinessImpact ?? BuildGenericApiImpact(apiFinding),
                Effort: apiFinding.Rule.Effort));
        }

        foreach (var packageFinding in result.PackageFindings.Where(finding => finding.IsBlocker))
        {
            insights.Add(new BlockerInsight(
                Priority: 90,
                Blocker: $"{packageFinding.PackageId} {packageFinding.RequestedVersion} ({packageFinding.ProjectName})",
                BusinessImpact: packageFinding.BusinessImpact ?? BuildGenericPackageImpact(packageFinding),
                Effort: packageFinding.Effort ?? "Medio"));
        }

        return insights
            .GroupBy(item => item.Blocker, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderByDescending(item => item.Priority)
            .ThenBy(item => item.Blocker, StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .Select(item => $"      <tr><td>{encoder.Encode(item.Blocker)}</td><td>{encoder.Encode(item.BusinessImpact)}</td><td>{encoder.Encode(item.Effort)}</td></tr>")
            .ToList();
    }

    private static string BuildGenericApiImpact(ApiFinding finding)
    {
        return $"{finding.Rule.Api} tende a concentrar retrabalho arquitetural, validações adicionais e maior risco de transição na jornada até .NET 10.";
    }

    private static string BuildGenericPackageImpact(PackageCompatibilityFinding finding)
    {
        return $"A dependência {finding.PackageId} não possui trilha clara de compatibilidade com .NET 10, o que tende a ampliar replanejamento técnico e validações extras.";
    }

    private static string BuildBandSummary(EconomicBand band)
    {
        return $"horas/semana {band.WeeklyHoursMin:0.##}-{band.WeeklyHoursMax:0.##}, equipe {band.TeamSizeMin:0.##}-{band.TeamSizeMax:0.##}, infraestrutura {FormatCurrencyRange(band.InfraCostMin, band.InfraCostMax)}, risco {band.RiskMultiplierMin:0.##}-{band.RiskMultiplierMax:0.##}x";
    }

    private static string FormatCurrencyRange(decimal min, decimal max)
    {
        return CostEstimator.Format(new MonthlyCostRange { Min = min, Max = max });
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

    private static IReadOnlyList<SolidLegendItem> BuildSolidLegend()
    {
        return
        [
            new SolidLegendItem("SRP", "Single Responsibility Principle", "Ajuda a identificar classes ou métodos que acumulam responsabilidades demais, elevando retrabalho, risco de defeitos e dificuldade de teste."),
            new SolidLegendItem("OCP", "Open/Closed Principle", "Aponta pontos em que a evolução do sistema tende a exigir alterações recorrentes no mesmo código, reduzindo previsibilidade de mudança."),
            new SolidLegendItem("LSP", "Liskov Substitution Principle", "Sinaliza heranças ou substituições potencialmente frágeis, que podem quebrar comportamentos esperados e dificultar reutilização segura."),
            new SolidLegendItem("ISP", "Interface Segregation Principle", "Mostra contratos grandes demais, que forçam consumidores a depender de capacidades que talvez nem usem, aumentando acoplamento."),
            new SolidLegendItem("DIP", "Dependency Inversion Principle", "Evidencia acoplamento excessivo a implementações concretas, o que costuma encarecer teste, troca de tecnologia e evolução arquitetural.")
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
                    .Where(item => !string.IsNullOrWhiteSpace(item))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3));
                var explanation = principles.Length > 1
                    ? $"O mesmo alvo concentra sinais simultâneos de {string.Join(", ", principles)}. Em termos gerenciais, isso sugere acoplamento estrutural combinado, baixa previsibilidade de mudança e maior risco de retrabalho."
                    : groupedFindings[0].Explanation;
                var confidence = principles.Length > 1
                    ? "Média"
                    : NormalizeConfidence(highestSeverity.Confidence);

                return new SolidOverviewRow(
                    string.Join(", ", principles),
                    target,
                    highestSeverity.Severity,
                    confidence,
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
            "SRP" => "Indica concentração excessiva de responsabilidade em pontos isolados do sistema.",
            "OCP" => "Sugere baixa flexibilidade para evoluir sem alterar código já estabilizado.",
            "LSP" => "Aponta risco de heranças ou substituições quebrarem comportamento esperado.",
            "ISP" => "Aponta contratos extensos, com consumo forçado de capacidades desnecessárias.",
            "DIP" => "Sinaliza dependência excessiva de implementações concretas e alto acoplamento.",
            _ => "Leitura heurística de fragilidade estrutural."
        };
    }

    private static string NormalizeConfidence(string confidence)
    {
        return confidence.Trim().ToLowerInvariant() switch
        {
            "alta" => "Alta",
            "média" => "Média",
            "media" => "Média",
            _ => "Baixa"
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
            "média" => 2,
            "media" => 2,
            _ => 1
        };
    }

    private sealed record SolidLegendItem(string Principle, string Meaning, string ManagerialReading);
    private sealed record SolidPrincipleSummary(string Principle, int Findings, int Targets, string Reading);
    private sealed record SolidOverviewRow(string Principles, string Target, string Severity, string Confidence, string Evidence, string Explanation, int PrincipleCount, int SeverityWeight);
    private sealed record BlockerInsight(int Priority, string Blocker, string BusinessImpact, string Effort);
}
