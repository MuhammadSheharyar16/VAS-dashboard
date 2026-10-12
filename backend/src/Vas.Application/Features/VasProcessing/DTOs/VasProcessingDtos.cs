using Vas.Application.Common.Reporting;

namespace Vas.Application.Features.VasProcessing.DTOs;

public sealed record VasProcessingQueryParams : ReportFilter;

public sealed record VasProcessingDto(
    ReportMetaDto Meta,
    IReadOnlyList<KpiCardDto> Kpis,
    IReadOnlyList<CountShareDto> Statuses,
    IReadOnlyList<CountShareDto> PendingByStage,
    IReadOnlyList<StageFunnelDto> StageFunnel,
    IReadOnlyList<BreakdownRowDto> Channels,
    IReadOnlyList<BreakdownRowDto> TurnaroundByVisaType);

/// <summary>How many applications reached a stage and how many have left it.</summary>
public sealed record StageFunnelDto(string Stage, int Reached, int Completed, decimal? CompletionPercent);
