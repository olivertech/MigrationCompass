using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Constrói um parecer gerencial determinístico a partir dos sinais técnicos encontrados no scan.
/// </summary>
public static class StrategyAdvisor
{
    /// <summary>
    /// Gera uma recomendação executiva com caminhos possíveis e drivers objetivos para decisão.
    /// </summary>
    public static SolutionAdvisory Build(SolutionScanResult result)
    {
        var totalProjects = Math.Max(result.Projects.Count, 1);
        var frameworkProjects = result.Projects.Count(project => project.MigrationProfile.Classification == ".NET Framework 3.x-4.x");
        var legacyCoreProjects = result.Projects.Count(project => project.MigrationProfile.Classification == ".NET Core 2.x/3.x");
        var highRiskProjects = result.Projects.Count(project => string.Equals(project.MigrationProfile.Impact, "Alto", StringComparison.OrdinalIgnoreCase));
        var blockerCount = result.Summary.CriticalBlockers;
        var legacyWebSignals = CountLegacyWebSignals(result);
        var blockerDensity = (double)blockerCount / totalProjects;
        var frameworkRatio = (double)frameworkProjects / totalProjects;

        var directMigrationIsWeak =
            frameworkRatio >= 0.5 ||
            blockerCount >= 6 ||
            result.Summary.RiskScore >= 85 ||
            legacyWebSignals >= 3;

        var rebuildCandidate =
            frameworkRatio >= 0.75 &&
            result.Summary.RiskScore >= 90 &&
            legacyWebSignals >= 2;

        var executiveHeadline = rebuildCandidate
            ? "A solução apresenta distância tecnológica elevada para uma migração direta até .NET 10."
            : directMigrationIsWeak
                ? "A solução exige modernização estruturada por etapas antes de qualquer salto relevante para .NET 10."
                : "A solução ainda demanda esforço relevante, mas pode ser tratada com uma estratégia progressiva de modernização.";

        var recommendedStrategy = rebuildCandidate
            ? "Reconstrução orientada por domínio, com convivência gradual entre legado e novos componentes."
            : directMigrationIsWeak
                ? "Modernização incremental, com fatiamento arquitetural e redução de acoplamentos antes da migração final."
                : "Migração progressiva com redução dirigida de dependências e validação faseada.";

        var rationale = rebuildCandidate
            ? "A combinação de legado em .NET Framework, alto volume de bloqueadores e forte presença de componentes web clássicos indica que insistir em uma migração direta tende a elevar custo, prazo e risco operacional além do razoável."
            : directMigrationIsWeak
                ? "Os sinais técnicos sugerem que uma migração direta seria frágil. O cenário favorece uma jornada em ondas, priorizando isolamento de dependências, estabilização funcional e redução de risco antes do alvo final."
                : "Embora existam gaps relevantes, a base atual ainda permite uma sequência de modernização progressiva, desde que a priorização recaia sobre dependências críticas e redução de complexidade estrutural.";

        var managerialPositioning = rebuildCandidate
            ? "Do ponto de vista gerencial, o caso se aproxima mais de uma decisão de reposicionamento tecnológico do que de uma simples atualização de versão."
            : directMigrationIsWeak
                ? "Do ponto de vista gerencial, o projeto pede uma decisão de investimento em transição controlada, e não apenas em upgrade técnico."
                : "Do ponto de vista gerencial, há espaço para modernização planejada, desde que o escopo seja controlado e a execução seja guiada por risco.";

        var distanceAssessment = BuildDistanceAssessment(frameworkProjects, legacyCoreProjects, highRiskProjects, totalProjects, legacyWebSignals);
        var opportunitySummary = BuildOpportunitySummary(frameworkProjects, legacyCoreProjects, legacyWebSignals);
        var decisionDrivers = BuildDecisionDrivers(result, frameworkProjects, blockerCount, legacyWebSignals, blockerDensity);
        var paths = BuildPaths(rebuildCandidate, directMigrationIsWeak);

        return new SolutionAdvisory
        {
            ExecutiveHeadline = executiveHeadline,
            RecommendedStrategy = recommendedStrategy,
            Rationale = rationale,
            ManagerialPositioning = managerialPositioning,
            DistanceAssessment = distanceAssessment,
            OpportunitySummary = opportunitySummary,
            DecisionDrivers = decisionDrivers,
            Paths = paths
        };
    }

    private static int CountLegacyWebSignals(SolutionScanResult result)
    {
        var packageSignals = result.PackageFindings.Count(finding =>
            finding.PackageId.StartsWith("Microsoft.AspNet.", StringComparison.OrdinalIgnoreCase) ||
            finding.PackageId.StartsWith("Microsoft.Owin", StringComparison.OrdinalIgnoreCase) ||
            finding.PackageId.Equals("Owin", StringComparison.OrdinalIgnoreCase));

        var apiSignals = result.ApiFindings.Count(finding =>
            finding.Rule.Api.StartsWith("System.Web", StringComparison.OrdinalIgnoreCase) ||
            finding.Rule.Api.StartsWith("System.ServiceModel", StringComparison.OrdinalIgnoreCase));

        return packageSignals + apiSignals;
    }

    private static string BuildDistanceAssessment(int frameworkProjects, int legacyCoreProjects, int highRiskProjects, int totalProjects, int legacyWebSignals)
    {
        if (frameworkProjects == totalProjects)
        {
            return $"Toda a amostra escaneada permanece em .NET Framework legado, com {legacyWebSignals} sinal(is) fortes de acoplamento a tecnologias web clássicas. A distância até o ecossistema moderno de .NET 10 deve ser tratada como estrutural, e não apenas operacional.";
        }

        if (frameworkProjects > 0 || legacyCoreProjects > 0)
        {
            return $"A solution combina {frameworkProjects} projeto(s) em .NET Framework e {legacyCoreProjects} em .NET Core legado, com {highRiskProjects} projeto(s) classificados em impacto alto. Isso sugere uma travessia tecnológica relevante, com necessidade de fatiamento e priorização.";
        }

        return $"A solution já está predominantemente no ecossistema unificado, mas ainda requer revisão de compatibilidade, dependências e governança de risco para sustentar a jornada até .NET 10.";
    }

    private static string BuildOpportunitySummary(int frameworkProjects, int legacyCoreProjects, int legacyWebSignals)
    {
        if (frameworkProjects > 0)
        {
            return "Ao permanecer em versões legadas, a organização deixa de capturar ganhos mais modernos de observabilidade, pipeline HTTP, segurança por padrão, simplificação de hospedagem, telemetria e governança operacional.";
        }

        if (legacyCoreProjects > 0 || legacyWebSignals > 0)
        {
            return "Mesmo sem estar presa ao .NET Framework em toda a solution, a base atual ainda limita padronização arquitetural, redução de custo operacional e adoção mais fluida de práticas modernas do ecossistema .NET.";
        }

        return "Os ganhos potenciais estão menos em ruptura tecnológica e mais em consolidação operacional, simplificação de dependências e alinhamento com a plataforma alvo.";
    }

    private static IReadOnlyList<string> BuildDecisionDrivers(SolutionScanResult result, int frameworkProjects, int blockerCount, int legacyWebSignals, double blockerDensity)
    {
        var drivers = new List<string>
        {
            $"{frameworkProjects} projeto(s) classificados em .NET Framework 3.x/4.x.",
            $"{blockerCount} bloqueador(es) crítico(s) relevante(s) identificado(s).",
            $"{legacyWebSignals} sinal(is) de forte dependência de componentes web legados.",
            $"Densidade média de bloqueadores: {blockerDensity:0.0} por projeto."
        };

        if (result.Summary.RiskScore >= 90)
        {
            drivers.Add($"Pontuação de risco em {result.Summary.RiskScore}/100, indicando baixa atratividade para salto direto de plataforma.");
        }
        else
        {
            drivers.Add($"Pontuação de risco em {result.Summary.RiskScore}/100, sugerindo necessidade de planejamento faseado.");
        }

        return drivers;
    }

    private static IReadOnlyList<DecisionPathOption> BuildPaths(bool rebuildCandidate, bool directMigrationIsWeak)
    {
        return
        [
            new DecisionPathOption
            {
                Title = "Migração direta para .NET 10",
                Fit = "Adequada apenas quando a base já está próxima do ecossistema unificado e com baixa densidade de bloqueadores.",
                Effort = "Alto em legado profundo; médio em bases mais novas",
                IndicativeRisk = "Alto em .NET Framework clássico",
                Guidance = "Deve ser considerada com cautela em bases muito antigas, principalmente quando houver forte acoplamento a System.Web, MVC 5, WebPages, OWIN ou autenticação legada.",
                IsRecommended = !directMigrationIsWeak
            },
            new DecisionPathOption
            {
                Title = "Modernização incremental por etapas",
                Fit = "Adequada para reduzir risco antes do salto final, isolando dependências e estabilizando fronteiras.",
                Effort = "Médio a alto",
                IndicativeRisk = "Controlável quando o escopo é fatiado",
                Guidance = "Costuma ser a abordagem mais equilibrada para ambientes que ainda precisam sustentar o legado enquanto reduzem acoplamentos técnicos.",
                IsRecommended = directMigrationIsWeak && !rebuildCandidate
            },
            new DecisionPathOption
            {
                Title = "Reconstrução gradual com convivência do legado",
                Fit = "Adequada quando a distância tecnológica é estrutural e a migração direta tende a consumir esforço desproporcional.",
                Effort = "Alto, porém mais previsível em legado extremo",
                IndicativeRisk = "Menor risco estrutural no médio prazo, desde que bem fatiada",
                Guidance = "Faz sentido quando o custo de adaptar o legado se aproxima do custo de construir uma nova base mais coerente com a plataforma alvo.",
                IsRecommended = rebuildCandidate
            }
        ];
    }
}
