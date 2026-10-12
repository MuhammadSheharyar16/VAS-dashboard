using Vas.Domain.Enums;

namespace Vas.Application.Common.Reporting;

/// <summary>
/// Turns per-day rows from the database into a chart series: one point per day for short ranges,
/// one per month for long ones, with a point for every period even when it has no rows.
/// </summary>
public static class TrendSeries
{
    /// <summary>Ranges up to this many days are charted by day; longer ones by month.</summary>
    public const int MaxDailyDays = 62;

    public static TrendGranularity GranularityFor(DateRange range) =>
        range.Days <= MaxDailyDays ? TrendGranularity.Day : TrendGranularity.Month;

    public static List<TPoint> Build<TRow, TPoint>(
        DateRange range,
        IEnumerable<TRow> dailyRows,
        Func<TRow, DateOnly> dayOf,
        Func<DateOnly, IReadOnlyList<TRow>, TPoint> createPoint)
    {
        var granularity = GranularityFor(range);
        var rowsByPeriod = dailyRows.ToLookup(row => PeriodStart(dayOf(row), granularity));

        return Periods(range, granularity)
            .Select(period => createPoint(period, rowsByPeriod[period].ToList()))
            .ToList();
    }

    private static DateOnly PeriodStart(DateOnly day, TrendGranularity granularity) =>
        granularity == TrendGranularity.Day ? day : new DateOnly(day.Year, day.Month, 1);

    private static IEnumerable<DateOnly> Periods(DateRange range, TrendGranularity granularity)
    {
        if (granularity == TrendGranularity.Day)
        {
            for (var day = range.From; day < range.ToExclusive; day = day.AddDays(1))
            {
                yield return day;
            }

            yield break;
        }

        var lastDay = range.ToExclusive.AddDays(-1);
        for (var month = PeriodStart(range.From, granularity); month <= lastDay; month = month.AddMonths(1))
        {
            yield return month;
        }
    }
}
