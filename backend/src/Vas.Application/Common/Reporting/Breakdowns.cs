using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;

namespace Vas.Application.Common.Reporting;

/// <summary>The display name of a code, and the group it belongs to (a country's region, a visa type's category).</summary>
public sealed record NamedItem(string Name, string? Group);

/// <summary>Application figures for one member of a breakdown: a country, a visa type, a channel.</summary>
public sealed record BreakdownRowDto(
    string Key,
    string Name,
    string? Group,
    int Received,
    int Processed,
    int Pending,
    int Approved,
    int Rejected,
    int Withdrawn,
    decimal? ApprovalRate,
    decimal? RejectionRate,
    decimal? AverageTurnaroundDays,
    decimal? SharePercent);

public static class Breakdowns
{
    /// <summary>Largest first. Rows whose code is missing from the lookup keep the code as their name.</summary>
    public static List<BreakdownRowDto> Build(
        IEnumerable<ApplicationStats> stats,
        IReadOnlyDictionary<string, NamedItem>? names = null)
    {
        var rows = stats.ToList();
        var total = rows.Sum(row => row.Received);

        return rows
            .OrderByDescending(row => row.Received)
            .ThenBy(row => row.Key, StringComparer.Ordinal)
            .Select(row =>
            {
                var named = names?.GetValueOrDefault(row.Key);
                return new BreakdownRowDto(
                    row.Key,
                    named?.Name ?? row.Key,
                    named?.Group,
                    row.Received,
                    row.Processed,
                    row.Pending,
                    row.Approved,
                    row.Rejected,
                    row.Withdrawn,
                    row.ApprovalRate,
                    row.RejectionRate,
                    row.TurnaroundDays,
                    KpiMath.Percent(row.Received, total));
            })
            .ToList();
    }
}

/// <summary>The small dimension tables, read whole to put names on codes.</summary>
public static class ReportLookups
{
    /// <summary>Dimension reads are capped; a dimension larger than this is not a dropdown.</summary>
    public const int MaxItems = 500;

    public static async Task<Dictionary<string, NamedItem>> CountryNamesAsync(
        this IReportingDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.Countries
            .OrderBy(c => c.CountryCode)
            .Take(MaxItems)
            .Select(c => new { c.CountryCode, c.CountryName, c.Region })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(c => c.CountryCode, c => new NamedItem(c.CountryName, c.Region));
    }

    public static async Task<Dictionary<string, NamedItem>> VisaTypeNamesAsync(
        this IReportingDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.VisaTypes
            .OrderBy(v => v.VisaTypeCode)
            .Take(MaxItems)
            .Select(v => new { v.VisaTypeCode, v.VisaTypeName, v.Category })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(v => v.VisaTypeCode, v => new NamedItem(v.VisaTypeName, v.Category));
    }

    public static async Task<Dictionary<string, NamedItem>> CentreNamesAsync(
        this IReportingDbContext db, CancellationToken cancellationToken)
    {
        var rows = await db.Centres
            .OrderBy(c => c.CentreCode)
            .Take(MaxItems)
            .Select(c => new { c.CentreCode, c.CentreName, c.CountryCode })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(c => c.CentreCode, c => new NamedItem(c.CentreName, c.CountryCode));
    }
}
