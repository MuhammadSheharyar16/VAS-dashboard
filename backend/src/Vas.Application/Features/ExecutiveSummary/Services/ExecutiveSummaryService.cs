using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Application.Features.ExecutiveSummary.Services;

/// <summary>
/// Every figure counts the applications submitted in the period (and the transactions dated in it).
/// The definitions are provisional until the KPI Mapping arrives; they are listed in docs/kpi-mapping.md.
/// </summary>
public sealed class ExecutiveSummaryService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IKpiCardService kpiCards,
    IValidator<ExecutiveSummaryQueryParams> validator) : IExecutiveSummaryService
{
    /// <summary>Ranges up to this many days are charted by day; longer ones by month.</summary>
    public const int MaxDailyTrendDays = 62;

    public const int TopCountryCount = 5;

    public async Task<ExecutiveSummaryDto> GetAsync(ExecutiveSummaryQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;

        var current = await LoadTotalsAsync(scope.Current, scope, cancellationToken);
        var previous = scope.Previous is null ? null : await LoadTotalsAsync(scope.Previous, scope, cancellationToken);

        return new ExecutiveSummaryDto(
            context.Meta,
            BuildKpis(current, previous),
            await LoadTrendAsync(scope, cancellationToken),
            BuildOutcomes(current.Applications),
            await LoadTopCountriesAsync(scope, current.Applications.Received, cancellationToken));
    }

    private sealed record ApplicationTotals(
        int Received,
        int Pending,
        int Approved,
        int Rejected,
        int Withdrawn,
        double? AverageTurnaroundDays)
    {
        public static ApplicationTotals Empty { get; } = new(0, 0, 0, 0, 0, null);

        public int Processed => Received - Pending;
    }

    private sealed record CurrencyTotal(string Currency, decimal Amount);

    private sealed record PeriodTotals(ApplicationTotals Applications, IReadOnlyList<CurrencyTotal> Revenue);

    private async Task<PeriodTotals> LoadTotalsAsync(DateRange range, ReportScope scope, CancellationToken cancellationToken)
    {
        var applications = await db.VisaApplications.InScope(range, scope)
            .GroupBy(_ => 1)
            .Select(group => new ApplicationTotals(
                group.Count(),
                group.Count(a => a.Status == ApplicationStatus.Pending),
                group.Count(a => a.Outcome == ApplicationOutcome.Approved),
                group.Count(a => a.Outcome == ApplicationOutcome.Rejected),
                group.Count(a => a.Outcome == ApplicationOutcome.Withdrawn),
                group.Average(a => (double?)a.TurnaroundDays)))
            .FirstOrDefaultAsync(cancellationToken);

        var revenue = await db.VasRevenue.InScope(range, scope)
            .GroupBy(t => t.Currency)
            .Select(group => new CurrencyTotal(group.Key, group.Sum(t => t.Amount)))
            .ToListAsync(cancellationToken);

        return new PeriodTotals(applications ?? ApplicationTotals.Empty, revenue);
    }

    private List<KpiCardDto> BuildKpis(PeriodTotals current, PeriodTotals? previous)
    {
        var now = current.Applications;
        var before = previous?.Applications;

        var cards = new List<KpiCardDto>
        {
            kpiCards.Create(KpiKeys.ApplicationsReceived, KpiUnit.Count, now.Received, before?.Received),
            kpiCards.Create(KpiKeys.ApplicationsProcessed, KpiUnit.Count, now.Processed, before?.Processed),
            kpiCards.Create(KpiKeys.ApplicationsPending, KpiUnit.Count, now.Pending, before?.Pending),
            kpiCards.Create(KpiKeys.ApprovalRate, KpiUnit.Percent, ApprovalRate(now), before is null ? null : ApprovalRate(before)),
            kpiCards.Create(KpiKeys.AverageTurnaroundDays, KpiUnit.Days, RoundDays(now), before is null ? null : RoundDays(before)),
        };

        // Currencies are never converted or added together: one revenue card per currency.
        var currencies = current.Revenue.Select(total => total.Currency)
            .Union(previous?.Revenue.Select(total => total.Currency) ?? [])
            .Order(StringComparer.Ordinal)
            .ToList();

        if (currencies.Count == 0)
        {
            cards.Add(kpiCards.Create(KpiKeys.VasRevenue, KpiUnit.Currency, 0, previous is null ? null : 0));
        }

        foreach (var currency in currencies)
        {
            cards.Add(kpiCards.Create(
                KpiKeys.VasRevenue,
                KpiUnit.Currency,
                AmountIn(current.Revenue, currency),
                previous is null ? null : AmountIn(previous.Revenue, currency),
                currency));
        }

        return cards;
    }

    /// <summary>Approved as a share of the applications that were approved or rejected.</summary>
    private static decimal? ApprovalRate(ApplicationTotals totals) =>
        KpiMath.Percent(totals.Approved, totals.Approved + totals.Rejected);

    private static decimal? RoundDays(ApplicationTotals totals) =>
        totals.AverageTurnaroundDays is { } days ? Math.Round((decimal)days, 1) : null;

    private static decimal AmountIn(IReadOnlyList<CurrencyTotal> totals, string currency) =>
        Math.Round(totals.Where(total => total.Currency == currency).Sum(total => total.Amount), 2);

    private static List<OutcomeShareDto> BuildOutcomes(ApplicationTotals totals) =>
    [
        new(ApplicationOutcome.Approved, totals.Approved, KpiMath.Percent(totals.Approved, totals.Received)),
        new(ApplicationOutcome.Rejected, totals.Rejected, KpiMath.Percent(totals.Rejected, totals.Received)),
        new(ApplicationOutcome.Withdrawn, totals.Withdrawn, KpiMath.Percent(totals.Withdrawn, totals.Received)),
        new(ApplicationStatus.Pending, totals.Pending, KpiMath.Percent(totals.Pending, totals.Received)),
    ];

    private async Task<ApplicationsTrendDto> LoadTrendAsync(ReportScope scope, CancellationToken cancellationToken)
    {
        var range = scope.Current;
        var applications = db.VisaApplications.InScope(range, scope);

        if (range.Days <= MaxDailyTrendDays)
        {
            var days = await applications
                .GroupBy(a => a.SubmittedDate)
                .Select(group => new TrendPointDto(group.Key, group.Count(), group.Count(a => a.Outcome != null)))
                .ToListAsync(cancellationToken);

            var byDay = days.ToDictionary(point => point.PeriodStart);

            // Days without data still appear, as zeros, so the chart shows the gap.
            var points = Enumerable.Range(0, range.Days)
                .Select(offset => range.From.AddDays(offset))
                .Select(day => byDay.GetValueOrDefault(day) ?? new TrendPointDto(day, 0, 0))
                .ToList();

            return new ApplicationsTrendDto(TrendGranularity.Day, points);
        }

        var months = await applications
            .GroupBy(a => new { a.SubmittedDate.Year, a.SubmittedDate.Month })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                Received = group.Count(),
                Decided = group.Count(a => a.Outcome != null),
            })
            .ToListAsync(cancellationToken);

        var byMonth = months.ToDictionary(month => new DateOnly(month.Year, month.Month, 1));
        var monthlyPoints = new List<TrendPointDto>();
        var lastDay = range.ToExclusive.AddDays(-1);

        for (var month = new DateOnly(range.From.Year, range.From.Month, 1); month <= lastDay; month = month.AddMonths(1))
        {
            var found = byMonth.GetValueOrDefault(month);
            monthlyPoints.Add(new TrendPointDto(month, found?.Received ?? 0, found?.Decided ?? 0));
        }

        return new ApplicationsTrendDto(TrendGranularity.Month, monthlyPoints);
    }

    private async Task<List<CountryVolumeDto>> LoadTopCountriesAsync(
        ReportScope scope,
        int totalReceived,
        CancellationToken cancellationToken)
    {
        var volumes = await db.VisaApplications.InScope(scope.Current, scope)
            .GroupBy(a => a.CountryCode)
            .Select(group => new { CountryCode = group.Key, Received = group.Count() })
            .OrderByDescending(volume => volume.Received)
            .ThenBy(volume => volume.CountryCode)
            .Take(TopCountryCount)
            .ToListAsync(cancellationToken);

        var countries = await db.Countries
            .Select(country => new { country.CountryCode, country.CountryName })
            .ToListAsync(cancellationToken);
        var names = countries.ToDictionary(country => country.CountryCode, country => country.CountryName);

        return volumes
            .Select(volume => new CountryVolumeDto(
                volume.CountryCode,
                names.GetValueOrDefault(volume.CountryCode, volume.CountryCode),
                volume.Received,
                KpiMath.Percent(volume.Received, totalReceived)))
            .ToList();
    }
}
