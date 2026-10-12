using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.VasApplications.DTOs;
using Vas.Domain.Enums;

namespace Vas.Application.Features.VasApplications.Services;

/// <summary>Visa applications and volumes: received, processed and pending, with trend and breakdowns.</summary>
public sealed class VasApplicationsService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IKpiCardService kpiCards,
    IValidator<VasApplicationsQueryParams> validator) : IVasApplicationsService
{
    public async Task<VasApplicationsDto> GetAsync(VasApplicationsQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;
        var applications = db.VisaApplications.InScope(scope.Current, scope);

        var now = await applications.LoadTotalAsync(cancellationToken);
        var before = scope.Previous is null
            ? null
            : await db.VisaApplications.InScope(scope.Previous, scope).LoadTotalAsync(cancellationToken);

        return new VasApplicationsDto(
            context.Meta,
            [
                kpiCards.Create(KpiKeys.ApplicationsReceived, KpiUnit.Count, now.Received, before?.Received),
                kpiCards.Create(KpiKeys.ApplicationsProcessed, KpiUnit.Count, now.Processed, before?.Processed),
                kpiCards.Create(KpiKeys.ApplicationsPending, KpiUnit.Count, now.Pending, before?.Pending),
            ],
            await applications.LoadApplicationsTrendAsync(scope.Current, cancellationToken),
            Breakdowns.Build(
                await applications.StatsBy(a => a.CountryCode).ToListAsync(cancellationToken),
                await db.CountryNamesAsync(cancellationToken)),
            Breakdowns.Build(
                await applications.StatsBy(a => a.VisaTypeCode).ToListAsync(cancellationToken),
                await db.VisaTypeNamesAsync(cancellationToken)),
            Breakdowns.Build(await applications.StatsBy(a => a.Channel).ToListAsync(cancellationToken)));
    }
}
