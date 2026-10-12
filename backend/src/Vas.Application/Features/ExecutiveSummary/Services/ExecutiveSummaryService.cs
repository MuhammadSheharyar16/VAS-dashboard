using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.Domain.Enums;

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
            await db.VisaApplications.InScope(scope.Current, scope).LoadApplicationsTrendAsync(scope.Current, cancellationToken),
            SharedReportBuilders.OutcomeShares(current.Applications),
            await LoadTopCountriesAsync(scope, cancellationToken));
    }

    private sealed record PeriodTotals(ApplicationStats Applications, IReadOnlyList<CurrencyAmount> Revenue);

    private async Task<PeriodTotals> LoadTotalsAsync(DateRange range, ReportScope scope, CancellationToken cancellationToken) =>
        new(
            await db.VisaApplications.InScope(range, scope).LoadTotalAsync(cancellationToken),
            await db.VasRevenue.InScope(range, scope).TotalsByCurrency().ToListAsync(cancellationToken));

    private List<KpiCardDto> BuildKpis(PeriodTotals current, PeriodTotals? previous)
    {
        var now = current.Applications;
        var before = previous?.Applications;

        return
        [
            kpiCards.Create(KpiKeys.ApplicationsReceived, KpiUnit.Count, now.Received, before?.Received),
            kpiCards.Create(KpiKeys.ApplicationsProcessed, KpiUnit.Count, now.Processed, before?.Processed),
            kpiCards.Create(KpiKeys.ApplicationsPending, KpiUnit.Count, now.Pending, before?.Pending),
            kpiCards.Create(KpiKeys.ApprovalRate, KpiUnit.Percent, now.ApprovalRate, before?.ApprovalRate),
            kpiCards.Create(KpiKeys.AverageTurnaroundDays, KpiUnit.Days, now.TurnaroundDays, before?.TurnaroundDays),
            .. kpiCards.CreateRevenueCards(current.Revenue, previous?.Revenue),
        ];
    }

    private async Task<List<CountryVolumeDto>> LoadTopCountriesAsync(ReportScope scope, CancellationToken cancellationToken)
    {
        var byCountry = await db.VisaApplications.InScope(scope.Current, scope)
            .StatsBy(a => a.CountryCode)
            .ToListAsync(cancellationToken);

        var names = await db.CountryNamesAsync(cancellationToken);

        return Breakdowns.Build(byCountry, names)
            .Take(TopCountryCount)
            .Select(row => new CountryVolumeDto(row.Key, row.Name, row.Received, row.SharePercent))
            .ToList();
    }
}
