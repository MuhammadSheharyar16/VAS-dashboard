using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.VasOutcomes.DTOs;
using Vas.Domain.Enums;

namespace Vas.Application.Features.VasOutcomes.Services;

/// <summary>Visa outcomes: approved, rejected and withdrawn, with approval and rejection rates.</summary>
public sealed class VasOutcomesService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IKpiCardService kpiCards,
    IValidator<VasOutcomesQueryParams> validator) : IVasOutcomesService
{
    public async Task<VasOutcomesDto> GetAsync(VasOutcomesQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;
        var applications = db.VisaApplications.InScope(scope.Current, scope);

        var now = await applications.LoadTotalAsync(cancellationToken);
        var before = scope.Previous is null
            ? null
            : await db.VisaApplications.InScope(scope.Previous, scope).LoadTotalAsync(cancellationToken);

        var days = await applications.DailyStats().ToListAsync(cancellationToken);
        var trend = TrendSeries.Build(
            scope.Current,
            days,
            day => day.Day,
            (period, rows) => new OutcomeTrendPointDto(
                period, rows.Sum(row => row.Approved), rows.Sum(row => row.Rejected), rows.Sum(row => row.Withdrawn)));

        return new VasOutcomesDto(
            context.Meta,
            [
                kpiCards.Create(KpiKeys.ApplicationsApproved, KpiUnit.Count, now.Approved, before?.Approved),
                kpiCards.Create(KpiKeys.ApplicationsRejected, KpiUnit.Count, now.Rejected, before?.Rejected),
                kpiCards.Create(KpiKeys.ApplicationsWithdrawn, KpiUnit.Count, now.Withdrawn, before?.Withdrawn),
                kpiCards.Create(KpiKeys.ApprovalRate, KpiUnit.Percent, now.ApprovalRate, before?.ApprovalRate),
                kpiCards.Create(KpiKeys.RejectionRate, KpiUnit.Percent, now.RejectionRate, before?.RejectionRate),
            ],
            new OutcomeTrendDto(TrendSeries.GranularityFor(scope.Current), trend),
            SharedReportBuilders.OutcomeShares(now),
            Breakdowns.Build(
                await applications.StatsBy(a => a.VisaTypeCode).ToListAsync(cancellationToken),
                await db.VisaTypeNamesAsync(cancellationToken)),
            Breakdowns.Build(
                await applications.StatsBy(a => a.CountryCode).ToListAsync(cancellationToken),
                await db.CountryNamesAsync(cancellationToken)));
    }
}
