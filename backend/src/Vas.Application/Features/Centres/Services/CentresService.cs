using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.Centres.DTOs;
using Vas.Domain.Enums;
using Vas.Shared.Results;

namespace Vas.Application.Features.Centres.Services;

/// <summary>
/// Centre performance: activity, processing volumes, turnaround and capacity use, as a paged table.
/// </summary>
/// <remarks>
/// The database returns one aggregated row per centre. Sorting and paging happen afterwards, because
/// capacity use is calculated from that row and the centre dimension; the row count is the number of
/// centres, never the number of applications.
/// </remarks>
public sealed class CentresService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IKpiCardService kpiCards,
    IOptions<PaginationOptions> pagination,
    IValidator<CentresQueryParams> validator) : ICentresService
{
    public async Task<CentresDto> GetAsync(CentresQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;

        var centreQuery = db.Centres.AsQueryable();
        if (scope.CountryCode is { } countryCode)
        {
            centreQuery = centreQuery.Where(centre => centre.CountryCode == countryCode);
        }

        if (scope.CentreCode is { } centreCode)
        {
            centreQuery = centreQuery.Where(centre => centre.CentreCode == centreCode);
        }

        var centres = await centreQuery
            .OrderBy(centre => centre.CentreCode)
            .Take(ReportLookups.MaxItems)
            .Select(centre => new
            {
                centre.CentreCode,
                centre.CentreName,
                centre.CountryCode,
                centre.City,
                centre.DailyCapacity,
            })
            .ToListAsync(cancellationToken);

        var stats = (await db.VisaApplications.InScope(scope.Current, scope)
                .StatsBy(a => a.CentreCode)
                .ToListAsync(cancellationToken))
            .ToDictionary(row => row.Key);

        var countries = await db.CountryNamesAsync(cancellationToken);
        var days = scope.Current.Days;

        var rows = centres.Select(centre =>
        {
            var centreStats = stats.GetValueOrDefault(centre.CentreCode) ?? ApplicationStats.Empty;
            var perDay = Math.Round(centreStats.Received / (decimal)days, 1);
            var utilisation = centre.DailyCapacity <= 0
                ? (decimal?)null
                : Math.Round(centreStats.Received * 100m / (days * centre.DailyCapacity), 1);

            return new CentreRowDto(
                centre.CentreCode,
                centre.CentreName,
                centre.CountryCode,
                countries.GetValueOrDefault(centre.CountryCode)?.Name ?? centre.CountryCode,
                centre.City,
                centre.DailyCapacity,
                centreStats.Received,
                centreStats.Processed,
                centreStats.Pending,
                centreStats.ApprovalRate,
                centreStats.TurnaroundDays,
                perDay,
                utilisation,
                kpiCards.StatusFor(KpiKeys.CapacityUtilisation, utilisation));
        });

        var sorted = Sort(rows, query.SortBy, query.SortDirection).ToList();

        var pageSize = Math.Clamp(query.PageSize ?? pagination.Value.DefaultPageSize, 1, pagination.Value.MaxPageSize);
        var page = Math.Max(query.Page, 1);
        var items = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new CentresDto(context.Meta, new PagedResponse<CentreRowDto>(items, sorted.Count, page, pageSize));
    }

    private static IEnumerable<CentreRowDto> Sort(IEnumerable<CentreRowDto> rows, string? sortBy, SortDirection direction)
    {
        Func<CentreRowDto, decimal?>? number = sortBy?.ToLowerInvariant() switch
        {
            "received" => row => row.Received,
            "pending" => row => row.Pending,
            "averageturnarounddays" => row => row.AverageTurnaroundDays,
            "capacityutilisation" => row => row.CapacityUtilisationPercent,
            _ => null,
        };

        var descending = direction == SortDirection.Descending;

        if (number is null)
        {
            return descending
                ? rows.OrderByDescending(row => row.CentreName, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.CentreCode, StringComparer.Ordinal)
                : rows.OrderBy(row => row.CentreName, StringComparer.OrdinalIgnoreCase).ThenBy(row => row.CentreCode, StringComparer.Ordinal);
        }

        // Centres without a value go last in either direction; the centre code breaks ties.
        var known = rows.OrderBy(row => number(row) is null);

        return (descending ? known.ThenByDescending(number) : known.ThenBy(number))
            .ThenBy(row => row.CentreCode, StringComparer.Ordinal);
    }
}
