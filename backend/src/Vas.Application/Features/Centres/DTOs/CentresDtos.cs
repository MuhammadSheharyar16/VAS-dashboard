using Vas.Application.Common.Reporting;
using Vas.Domain.Enums;
using Vas.Shared.Results;

namespace Vas.Application.Features.Centres.DTOs;

public sealed record CentresQueryParams : ReportFilter
{
    public int Page { get; init; } = 1;

    /// <summary>Left out, the configured default applies. Larger than the configured maximum, the maximum applies.</summary>
    public int? PageSize { get; init; }

    /// <summary>One of <see cref="CentreSortFields.All"/>. Left out, centres are sorted by name.</summary>
    public string? SortBy { get; init; }

    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
}

public static class CentreSortFields
{
    public const string CentreName = "centreName";
    public const string Received = "received";
    public const string Pending = "pending";
    public const string AverageTurnaroundDays = "averageTurnaroundDays";
    public const string CapacityUtilisation = "capacityUtilisation";

    public static IReadOnlyList<string> All { get; } =
        [CentreName, Received, Pending, AverageTurnaroundDays, CapacityUtilisation];
}

public sealed record CentresDto(ReportMetaDto Meta, PagedResponse<CentreRowDto> Centres);

public sealed record CentreRowDto(
    string CentreCode,
    string CentreName,
    string CountryCode,
    string CountryName,
    string City,
    int DailyCapacity,
    int Received,
    int Processed,
    int Pending,
    decimal? ApprovalRate,
    decimal? AverageTurnaroundDays,
    decimal AverageDailyApplications,
    decimal? CapacityUtilisationPercent,
    KpiStatus CapacityStatus);
