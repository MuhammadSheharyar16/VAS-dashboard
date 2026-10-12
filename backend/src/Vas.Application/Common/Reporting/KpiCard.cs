using Microsoft.Extensions.Options;
using Vas.Application.Common.Options;
using Vas.Domain.Enums;

namespace Vas.Application.Common.Reporting;

/// <summary>
/// A dashboard-ready KPI. The client shows these values as they are and never recalculates them.
/// </summary>
public sealed record KpiCardDto(
    string Key,
    decimal? Value,
    KpiUnit Unit,
    string? Currency,
    decimal? PreviousValue,
    decimal? ChangePercent,
    TrendDirection Trend,
    KpiStatus Status);

public interface IKpiCardService
{
    KpiCardDto Create(string key, KpiUnit unit, decimal? value, decimal? previousValue, string? currency = null);
}

public sealed class KpiCardService(IOptions<KpiThresholdOptions> thresholds) : IKpiCardService
{
    public KpiCardDto Create(string key, KpiUnit unit, decimal? value, decimal? previousValue, string? currency = null)
    {
        var change = ChangePercent(value, previousValue);

        return new KpiCardDto(
            key,
            value,
            unit,
            currency,
            previousValue,
            change,
            change switch
            {
                > 0 => TrendDirection.Up,
                < 0 => TrendDirection.Down,
                _ => TrendDirection.Flat,
            },
            StatusFor(key, value));
    }

    public static decimal? ChangePercent(decimal? value, decimal? previousValue) =>
        value is null || previousValue is null or 0
            ? null
            : Math.Round((value.Value - previousValue.Value) / Math.Abs(previousValue.Value) * 100, 1);

    private KpiStatus StatusFor(string key, decimal? value)
    {
        if (value is null || !thresholds.Value.Kpis.TryGetValue(key, out var threshold))
        {
            return KpiStatus.Neutral;
        }

        if (threshold.HigherIsBetter)
        {
            return value <= threshold.Critical ? KpiStatus.Critical
                : value <= threshold.Warning ? KpiStatus.Warning
                : KpiStatus.Good;
        }

        return value >= threshold.Critical ? KpiStatus.Critical
            : value >= threshold.Warning ? KpiStatus.Warning
            : KpiStatus.Good;
    }
}

public static class KpiKeys
{
    public const string ApplicationsReceived = "applicationsReceived";
    public const string ApplicationsProcessed = "applicationsProcessed";
    public const string ApplicationsPending = "applicationsPending";
    public const string ApprovalRate = "approvalRate";
    public const string AverageTurnaroundDays = "averageTurnaroundDays";
    public const string VasRevenue = "vasRevenue";
}

public static class KpiMath
{
    /// <summary>A share as a percentage with one decimal, or null when there is nothing to divide by.</summary>
    public static decimal? Percent(int part, int whole) =>
        whole == 0 ? null : Math.Round(part * 100m / whole, 1);
}
