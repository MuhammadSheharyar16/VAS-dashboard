using Vas.Application.Common.Reporting;
using Vas.Domain.Enums;

namespace Vas.Application.Features.VasOutcomes.DTOs;

public sealed record VasOutcomesQueryParams : ReportFilter;

public sealed record VasOutcomesDto(
    ReportMetaDto Meta,
    IReadOnlyList<KpiCardDto> Kpis,
    OutcomeTrendDto Trend,
    IReadOnlyList<OutcomeShareDto> Outcomes,
    IReadOnlyList<BreakdownRowDto> ByVisaType,
    IReadOnlyList<BreakdownRowDto> ByCountry);

public sealed record OutcomeTrendDto(TrendGranularity Granularity, IReadOnlyList<OutcomeTrendPointDto> Points);

public sealed record OutcomeTrendPointDto(DateOnly PeriodStart, int Approved, int Rejected, int Withdrawn);
