using System.Linq.Expressions;
using Vas.Domain.Reporting;

namespace Vas.Application.Common.Reporting;

/// <summary>The application counts every report is built from, for one group (a country, a visa type, the whole scope).</summary>
public sealed record ApplicationStats(
    string Key,
    int Received,
    int Pending,
    int Approved,
    int Rejected,
    int Withdrawn,
    double? AverageTurnaroundDays)
{
    public static ApplicationStats Empty { get; } = new(string.Empty, 0, 0, 0, 0, 0, null);

    public int Processed => Received - Pending;

    /// <summary>Approved as a share of the applications that were approved or rejected.</summary>
    public decimal? ApprovalRate => KpiMath.Percent(Approved, Approved + Rejected);

    /// <summary>Rejected as a share of the applications that were approved or rejected.</summary>
    public decimal? RejectionRate => KpiMath.Percent(Rejected, Approved + Rejected);

    public decimal? TurnaroundDays =>
        AverageTurnaroundDays is { } days ? Math.Round((decimal)days, 1) : null;
}

public sealed record DailyApplicationStats(DateOnly Day, int Received, int Approved, int Rejected, int Withdrawn)
{
    public int Decided => Approved + Rejected + Withdrawn;
}

public static class ApplicationStatsQueries
{
    /// <summary>One row of counts for everything in the query, or no row when the query is empty.</summary>
    public static IQueryable<ApplicationStats> TotalStats(this IQueryable<VisaApplication> applications) =>
        applications
            .GroupBy(_ => 1)
            .Select(group => new ApplicationStats(
                string.Empty,
                group.Count(),
                group.Count(a => a.Status == ApplicationStatus.Pending),
                group.Count(a => a.Outcome == ApplicationOutcome.Approved),
                group.Count(a => a.Outcome == ApplicationOutcome.Rejected),
                group.Count(a => a.Outcome == ApplicationOutcome.Withdrawn),
                group.Average(a => (double?)a.TurnaroundDays)));

    /// <summary>One row of counts per value of <paramref name="key"/>.</summary>
    public static IQueryable<ApplicationStats> StatsBy(
        this IQueryable<VisaApplication> applications,
        Expression<Func<VisaApplication, string>> key) =>
        applications
            .GroupBy(key)
            .Select(group => new ApplicationStats(
                group.Key,
                group.Count(),
                group.Count(a => a.Status == ApplicationStatus.Pending),
                group.Count(a => a.Outcome == ApplicationOutcome.Approved),
                group.Count(a => a.Outcome == ApplicationOutcome.Rejected),
                group.Count(a => a.Outcome == ApplicationOutcome.Withdrawn),
                group.Average(a => (double?)a.TurnaroundDays)));

    /// <summary>One row of counts per submission day.</summary>
    public static IQueryable<DailyApplicationStats> DailyStats(this IQueryable<VisaApplication> applications) =>
        applications
            .GroupBy(a => a.SubmittedDate)
            .Select(group => new DailyApplicationStats(
                group.Key,
                group.Count(),
                group.Count(a => a.Outcome == ApplicationOutcome.Approved),
                group.Count(a => a.Outcome == ApplicationOutcome.Rejected),
                group.Count(a => a.Outcome == ApplicationOutcome.Withdrawn)));
}
