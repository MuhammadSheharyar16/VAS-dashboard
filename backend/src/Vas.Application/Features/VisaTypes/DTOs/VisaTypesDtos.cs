using Vas.Application.Common.Reporting;

namespace Vas.Application.Features.VisaTypes.DTOs;

public sealed record VisaTypesQueryParams : ReportFilter;

/// <summary>Volumes and performance for every visa type in scope, including those with no applications.</summary>
public sealed record VisaTypesDto(ReportMetaDto Meta, IReadOnlyList<BreakdownRowDto> VisaTypes);
