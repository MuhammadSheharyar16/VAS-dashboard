using Vas.Application.Common.Reporting;
using Vas.Domain.Enums;

namespace Vas.Application.Features.VasRevenue.DTOs;

public sealed record VasRevenueQueryParams : ReportFilter;

public sealed record VasRevenueDto(
    ReportMetaDto Meta,
    IReadOnlyList<KpiCardDto> Kpis,
    RevenueTrendDto Trend,
    IReadOnlyList<RevenueBreakdownRowDto> ByCountry,
    IReadOnlyList<RevenueBreakdownRowDto> ByCentre,
    IReadOnlyList<RevenueBreakdownRowDto> ByVisaType,
    IReadOnlyList<RevenueBreakdownRowDto> ByService);

public sealed record RevenueTrendDto(TrendGranularity Granularity, IReadOnlyList<RevenueTrendPointDto> Points);

/// <summary>Revenue for one period in one currency. Each currency in the range has a point for every period.</summary>
public sealed record RevenueTrendPointDto(DateOnly PeriodStart, string Currency, decimal Amount, int Transactions);

/// <summary>
/// Revenue for one member of a breakdown, in one currency. <see cref="SharePercent"/> is the share
/// of that currency's total, because currencies are never added together.
/// </summary>
public sealed record RevenueBreakdownRowDto(
    string Key,
    string Name,
    string Currency,
    decimal Amount,
    int Transactions,
    decimal? SharePercent);
