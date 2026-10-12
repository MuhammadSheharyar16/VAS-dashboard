using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.VasProcessing.DTOs;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Application.Features.VasProcessing.Services;

/// <summary>
/// Visa processing and journey: application status, processing stages, the digital and centre
/// journeys, and turnaround times.
/// </summary>
/// <remarks>
/// Time spent in each stage is not reported: it needs a duration the Gold layer does not provide yet,
/// and date arithmetic is not something both data sources can run from one query.
/// </remarks>
public sealed class VasProcessingService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IKpiCardService kpiCards,
    IValidator<VasProcessingQueryParams> validator) : IVasProcessingService
{
    private sealed record StageCount(string Stage, int Reached, int Completed);

    public async Task<VasProcessingDto> GetAsync(VasProcessingQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;
        var applications = db.VisaApplications.InScope(scope.Current, scope);

        var now = await applications.LoadTotalAsync(cancellationToken);
        var channels = await applications.StatsBy(a => a.Channel).ToListAsync(cancellationToken);

        ApplicationStats? before = null;
        List<ApplicationStats>? channelsBefore = null;
        if (scope.Previous is not null)
        {
            var previous = db.VisaApplications.InScope(scope.Previous, scope);
            before = await previous.LoadTotalAsync(cancellationToken);
            channelsBefore = await previous.StatsBy(a => a.Channel).ToListAsync(cancellationToken);
        }

        var statuses = await applications
            .GroupBy(a => a.Status)
            .Select(group => new KeyCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var pendingByStage = await applications
            .Where(a => a.Status == ApplicationStatus.Pending)
            .GroupBy(a => a.CurrentStage)
            .Select(group => new KeyCount(group.Key, group.Count()))
            .ToListAsync(cancellationToken);

        var stageCounts = await db.ApplicationStages
            .Join(applications, stage => stage.ApplicationId, a => a.ApplicationId, (stage, _) => stage)
            .GroupBy(stage => stage.Stage)
            .Select(group => new StageCount(group.Key, group.Count(), group.Count(stage => stage.ExitedAtUtc != null)))
            .ToListAsync(cancellationToken);

        return new VasProcessingDto(
            context.Meta,
            BuildKpis(now, before, channels, channelsBefore),
            SharedReportBuilders.CountShares(statuses),
            SharedReportBuilders.CountShares(pendingByStage, ApplicationStageName.Ordered),
            BuildFunnel(stageCounts),
            Breakdowns.Build(channels),
            Breakdowns.Build(
                await applications.StatsBy(a => a.VisaTypeCode).ToListAsync(cancellationToken),
                await db.VisaTypeNamesAsync(cancellationToken)));
    }

    private List<KpiCardDto> BuildKpis(
        ApplicationStats now,
        ApplicationStats? before,
        List<ApplicationStats> channels,
        List<ApplicationStats>? channelsBefore)
    {
        var digital = Channel(channels, ApplicationChannel.Digital);
        var centre = Channel(channels, ApplicationChannel.Centre);
        var digitalBefore = channelsBefore is null ? null : Channel(channelsBefore, ApplicationChannel.Digital);
        var centreBefore = channelsBefore is null ? null : Channel(channelsBefore, ApplicationChannel.Centre);

        return
        [
            kpiCards.Create(KpiKeys.AverageTurnaroundDays, KpiUnit.Days, now.TurnaroundDays, before?.TurnaroundDays),
            kpiCards.Create(KpiKeys.DigitalTurnaroundDays, KpiUnit.Days, digital.TurnaroundDays, digitalBefore?.TurnaroundDays),
            kpiCards.Create(KpiKeys.CentreTurnaroundDays, KpiUnit.Days, centre.TurnaroundDays, centreBefore?.TurnaroundDays),
            kpiCards.Create(
                KpiKeys.DigitalSharePercent,
                KpiUnit.Percent,
                KpiMath.Percent(digital.Received, now.Received),
                before is null ? null : KpiMath.Percent(digitalBefore!.Received, before.Received)),
        ];
    }

    private static ApplicationStats Channel(List<ApplicationStats> channels, string channel) =>
        channels.FirstOrDefault(stats => stats.Key == channel) ?? ApplicationStats.Empty;

    private static List<StageFunnelDto> BuildFunnel(List<StageCount> counts)
    {
        var order = ApplicationStageName.Ordered.ToList();

        int Position(string stage)
        {
            var index = order.IndexOf(stage);
            return index < 0 ? int.MaxValue : index;
        }

        return counts
            .OrderBy(count => Position(count.Stage))
            .ThenBy(count => count.Stage, StringComparer.Ordinal)
            .Select(count => new StageFunnelDto(
                count.Stage, count.Reached, count.Completed, KpiMath.Percent(count.Completed, count.Reached)))
            .ToList();
    }
}
