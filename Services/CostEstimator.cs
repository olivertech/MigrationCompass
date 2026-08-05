using System.Globalization;
using MigrationCompass.Models;

namespace MigrationCompass.Services;

/// <summary>
/// Calcula ranges mensais orientativos a partir de premissas econômicas configuráveis.
/// </summary>
public sealed class CostEstimator(EconomicParameters parameters)
{
    private readonly EconomicParameters _parameters = parameters;

    /// <summary>
    /// Estima o custo mensal orientativo para uma regra de API.
    /// </summary>
    public MonthlyCostRange Estimate(ApiRule rule)
    {
        return Estimate(rule.Impact, rule.Effort, rule.EconomicProfile);
    }

    /// <summary>
    /// Estima o custo mensal orientativo para um achado de pacote.
    /// </summary>
    public MonthlyCostRange Estimate(PackageCompatibilityFinding finding)
    {
        return Estimate(finding.Impact, finding.Effort, finding.EconomicProfile);
    }

    /// <summary>
    /// Formata um range mensal em moeda brasileira.
    /// </summary>
    public static string Format(MonthlyCostRange range)
    {
        var culture = new CultureInfo("pt-BR");
        var min = Math.Round(range.Min, 0, MidpointRounding.AwayFromZero);
        var max = Math.Round(range.Max, 0, MidpointRounding.AwayFromZero);
        return $"{min.ToString("C0", culture)} a {max.ToString("C0", culture)}";
    }

    private MonthlyCostRange Estimate(string impact, string? effort, EconomicProfile? profile)
    {
        var band = SelectBand(impact);
        var weeklyHoursMin = profile?.WeeklyHoursMin ?? band.WeeklyHoursMin;
        var weeklyHoursMax = profile?.WeeklyHoursMax ?? band.WeeklyHoursMax;
        var teamSizeMin = profile?.TeamSizeMin ?? AdjustTeamSizeMin(band.TeamSizeMin, effort);
        var teamSizeMax = profile?.TeamSizeMax ?? AdjustTeamSizeMax(band.TeamSizeMax, effort);
        var infraCostMin = profile?.InfraCostMin ?? band.InfraCostMin;
        var infraCostMax = profile?.InfraCostMax ?? band.InfraCostMax;
        var riskMultiplierMin = profile?.RiskMultiplierMin ?? AdjustRiskMin(band.RiskMultiplierMin, effort);
        var riskMultiplierMax = profile?.RiskMultiplierMax ?? AdjustRiskMax(band.RiskMultiplierMax, effort);

        var devMin = _parameters.HourlyRateMin * weeklyHoursMin * teamSizeMin * _parameters.WeeksPerMonth;
        var devMax = _parameters.HourlyRateMax * weeklyHoursMax * teamSizeMax * _parameters.WeeksPerMonth;

        return new MonthlyCostRange
        {
            Min = (devMin + infraCostMin) * riskMultiplierMin,
            Max = (devMax + infraCostMax) * riskMultiplierMax
        };
    }

    private EconomicBand SelectBand(string impact)
    {
        return NormalizeLevel(impact) switch
        {
            "alto" => _parameters.High,
            "baixo" => _parameters.Low,
            _ => _parameters.Medium
        };
    }

    private static decimal AdjustTeamSizeMin(decimal baseValue, string? effort)
    {
        return NormalizeLevel(effort) switch
        {
            "alto" => baseValue + 0.5m,
            "baixo" => Math.Max(1m, baseValue - 0.25m),
            _ => baseValue
        };
    }

    private static decimal AdjustTeamSizeMax(decimal baseValue, string? effort)
    {
        return NormalizeLevel(effort) switch
        {
            "alto" => baseValue + 0.5m,
            "baixo" => Math.Max(1m, baseValue - 0.25m),
            _ => baseValue
        };
    }

    private static decimal AdjustRiskMin(decimal baseValue, string? effort)
    {
        return NormalizeLevel(effort) switch
        {
            "alto" => baseValue + 0.10m,
            "baixo" => Math.Max(1.0m, baseValue - 0.05m),
            _ => baseValue
        };
    }

    private static decimal AdjustRiskMax(decimal baseValue, string? effort)
    {
        return NormalizeLevel(effort) switch
        {
            "alto" => baseValue + 0.15m,
            "baixo" => Math.Max(1.0m, baseValue - 0.05m),
            _ => baseValue
        };
    }

    private static string NormalizeLevel(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "alto" => "alto",
            "médio" => "medio",
            "medio" => "medio",
            "baixo" => "baixo",
            _ => "medio"
        };
    }
}
