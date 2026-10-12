using Vas.Application.Common.Reporting;

namespace Vas.Application.Features.ExecutiveSummary.DTOs;

public sealed record ExecutiveSummaryDto(
    ReportMetaDto Meta,
    IReadOnlyList<KpiCardDto> Kpis,
    ApplicationsTrendDto ApplicationsTrend,
    IReadOnlyList<OutcomeShareDto> Outcomes,
    IReadOnlyList<CountryVolumeDto> TopCountries);

public sealed record CountryVolumeDto(string CountryCode, string CountryName, int Received, decimal? SharePercent);
