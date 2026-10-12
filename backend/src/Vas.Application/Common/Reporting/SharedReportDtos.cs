using Microsoft.EntityFrameworkCore;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Application.Common.Reporting;

public sealed record ApplicationsTrendDto(TrendGranularity Granularity, IReadOnlyList<TrendPointDto> Points);

/// <summary>One day or one month. <see cref="PeriodStart"/> is the day, or the first day of the month.</summary>
public sealed record TrendPointDto(DateOnly PeriodStart, int Received, int Decided);

public sealed record OutcomeShareDto(string Outcome, int Count, decimal? SharePercent);

/// <summary>A count for one value of a column, with its share of the total.</summary>
public sealed record CountShareDto(string Key, int Count, decimal? SharePercent);

public sealed record KeyCount(string Key, int Count);

public static class SharedReportBuilders
{
    public static async Task<ApplicationStats> LoadTotalAsync(
        this IQueryable<VisaApplication> applications, CancellationToken cancellationToken) =>
        await applications.TotalStats().FirstOrDefaultAsync(cancellationToken) ?? ApplicationStats.Empty;

    public static async Task<ApplicationsTrendDto> LoadApplicationsTrendAsync(
        this IQueryable<VisaApplication> applications, DateRange range, CancellationToken cancellationToken)
    {
        var days = await applications.DailyStats().ToListAsync(cancellationToken);

        var points = TrendSeries.Build(
            range,
            days,
            day => day.Day,
            (period, rows) => new TrendPointDto(period, rows.Sum(row => row.Received), rows.Sum(row => row.Decided)));

        return new ApplicationsTrendDto(TrendSeries.GranularityFor(range), points);
    }

    public static List<OutcomeShareDto> OutcomeShares(ApplicationStats totals) =>
    [
        new(ApplicationOutcome.Approved, totals.Approved, KpiMath.Percent(totals.Approved, totals.Received)),
        new(ApplicationOutcome.Rejected, totals.Rejected, KpiMath.Percent(totals.Rejected, totals.Received)),
        new(ApplicationOutcome.Withdrawn, totals.Withdrawn, KpiMath.Percent(totals.Withdrawn, totals.Received)),
        new(ApplicationStatus.Pending, totals.Pending, KpiMath.Percent(totals.Pending, totals.Received)),
    ];

    public static List<CountShareDto> CountShares(IEnumerable<KeyCount> counts, IReadOnlyList<string>? order = null)
    {
        var rows = counts.ToList();
        var total = rows.Sum(row => row.Count);

        int Position(string key)
        {
            var index = order is null ? -1 : order.ToList().IndexOf(key);
            return index < 0 ? int.MaxValue : index;
        }

        var sorted = order is null
            ? rows.OrderByDescending(row => row.Count).ThenBy(row => row.Key, StringComparer.Ordinal)
            : rows.OrderBy(row => Position(row.Key)).ThenBy(row => row.Key, StringComparer.Ordinal);

        return sorted.Select(row => new CountShareDto(row.Key, row.Count, KpiMath.Percent(row.Count, total))).ToList();
    }
}
