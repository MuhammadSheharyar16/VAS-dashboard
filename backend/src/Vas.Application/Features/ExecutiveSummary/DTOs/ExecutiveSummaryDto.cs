using Vas.Application.Common.Reporting;
using Vas.Domain.Enums;

namespace Vas.Application.Features.ExecutiveSummary.DTOs;

public sealed record ExecutiveSummaryDto(
    ReportMetaDto Meta,
    IReadOnlyList<KpiCardDto> Kpis,
    ApplicationsTrendDto ApplicationsTrend,
    IReadOnlyList<OutcomeShareDto> Outcomes,
    IReadOnlyList<CountryVolumeDto> TopCountries);

public sealed record ApplicationsTrendDto(TrendGranularity Granularity, IReadOnlyList<TrendPointDto> Points);

/// <summary>One day or one month. <see cref="PeriodStart"/> is the day, or the first day of the month.</summary>
public sealed record TrendPointDto(DateOnly PeriodStart, int Received, int Decided);

public sealed record OutcomeShareDto(string Outcome, int Count, decimal? SharePercent);

public sealed record CountryVolumeDto(string CountryCode, string CountryName, int Received, decimal? SharePercent);
