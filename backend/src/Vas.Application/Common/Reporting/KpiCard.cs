using Microsoft.Extensions.Options;
using Vas.Application.Common.Options;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

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

/// <summary>Revenue and transaction count in one currency.</summary>
public sealed record CurrencyAmount(string Currency, decimal Amount, int Transactions);

public interface IKpiCardService
{
    KpiCardDto Create(string key, KpiUnit unit, decimal? value, decimal? previousValue, string? currency = null);

    /// <summary>One revenue card per currency. Currencies are never converted or added together.</summary>
    IReadOnlyList<KpiCardDto> CreateRevenueCards(
        IReadOnlyList<CurrencyAmount> current,
        IReadOnlyList<CurrencyAmount>? previous);

    /// <summary>The status of a value against the configured threshold for its KPI, or Neutral when there is none.</summary>
    KpiStatus StatusFor(string key, decimal? value);
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

    public IReadOnlyList<KpiCardDto> CreateRevenueCards(
        IReadOnlyList<CurrencyAmount> current,
        IReadOnlyList<CurrencyAmount>? previous)
    {
        var currencies = current.Select(total => total.Currency)
            .Union(previous?.Select(total => total.Currency) ?? [])
            .Order(StringComparer.Ordinal)
            .ToList();

        if (currencies.Count == 0)
        {
            return [Create(KpiKeys.VasRevenue, KpiUnit.Currency, 0, previous is null ? null : 0)];
        }

        return currencies
            .Select(currency => Create(
                KpiKeys.VasRevenue,
                KpiUnit.Currency,
                AmountIn(current, currency),
                previous is null ? null : AmountIn(previous, currency),
                currency))
            .ToList();
    }

    public KpiStatus StatusFor(string key, decimal? value)
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

    public static decimal? ChangePercent(decimal? value, decimal? previousValue) =>
        value is null || previousValue is null or 0
            ? null
            : Math.Round((value.Value - previousValue.Value) / Math.Abs(previousValue.Value) * 100, 1);

    private static decimal AmountIn(IReadOnlyList<CurrencyAmount> totals, string currency) =>
        Math.Round(totals.Where(total => total.Currency == currency).Sum(total => total.Amount), 2);
}

public static class KpiKeys
{
    public const string ApplicationsReceived = "applicationsReceived";
    public const string ApplicationsProcessed = "applicationsProcessed";
    public const string ApplicationsPending = "applicationsPending";
    public const string ApplicationsApproved = "applicationsApproved";
    public const string ApplicationsRejected = "applicationsRejected";
    public const string ApplicationsWithdrawn = "applicationsWithdrawn";
    public const string ApprovalRate = "approvalRate";
    public const string RejectionRate = "rejectionRate";
    public const string AverageTurnaroundDays = "averageTurnaroundDays";
    public const string DigitalTurnaroundDays = "digitalTurnaroundDays";
    public const string CentreTurnaroundDays = "centreTurnaroundDays";
    public const string DigitalSharePercent = "digitalSharePercent";
    public const string VasRevenue = "vasRevenue";
    public const string RevenueTransactions = "revenueTransactions";
    public const string CapacityUtilisation = "capacityUtilisation";
}

public static class KpiMath
{
    /// <summary>A share as a percentage with one decimal, or null when there is nothing to divide by.</summary>
    public static decimal? Percent(int part, int whole) =>
        whole == 0 ? null : Math.Round(part * 100m / whole, 1);

    /// <summary>A share of an amount as a percentage with one decimal, or null when the whole is zero.</summary>
    public static decimal? Percent(decimal part, decimal whole) =>
        whole == 0 ? null : Math.Round(part * 100m / whole, 1);
}

public static class RevenueQueries
{
    public static IQueryable<CurrencyAmount> TotalsByCurrency(this IQueryable<VasRevenueTransaction> transactions) =>
        transactions
            .GroupBy(t => t.Currency)
            .Select(group => new CurrencyAmount(group.Key, group.Sum(t => t.Amount), group.Count()));
}
