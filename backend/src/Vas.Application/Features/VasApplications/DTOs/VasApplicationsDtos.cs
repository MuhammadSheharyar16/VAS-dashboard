using Vas.Application.Common.Reporting;

namespace Vas.Application.Features.VasApplications.DTOs;

public sealed record VasApplicationsQueryParams : ReportFilter;

public sealed record VasApplicationsDto(
    ReportMetaDto Meta,
    IReadOnlyList<KpiCardDto> Kpis,
    ApplicationsTrendDto Trend,
    IReadOnlyList<BreakdownRowDto> ByCountry,
    IReadOnlyList<BreakdownRowDto> ByVisaType,
    IReadOnlyList<BreakdownRowDto> ByChannel);
