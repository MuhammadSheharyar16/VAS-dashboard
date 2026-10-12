using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.VasRevenue.DTOs;
using Vas.Domain.Enums;

namespace Vas.Application.Features.VasRevenue.Services;

/// <summary>VAS revenue, with breakdowns by country, centre, visa type and service. Never converts currencies.</summary>
public sealed class VasRevenueService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IKpiCardService kpiCards,
    IValidator<VasRevenueQueryParams> validator) : IVasRevenueService
{
    private sealed record RevenueGroup(string Key, string Currency, decimal Amount, int Transactions);

    private sealed record DailyRevenue(DateOnly Day, string Currency, decimal Amount, int Transactions);

    public async Task<VasRevenueDto> GetAsync(VasRevenueQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;
        var transactions = db.VasRevenue.InScope(scope.Current, scope);

        var now = await transactions.TotalsByCurrency().ToListAsync(cancellationToken);
        var before = scope.Previous is null
            ? null
            : await db.VasRevenue.InScope(scope.Previous, scope).TotalsByCurrency().ToListAsync(cancellationToken);

        var byCountry = await transactions
            .GroupBy(t => new { Key = t.CountryCode, t.Currency })
            .Select(group => new RevenueGroup(group.Key.Key, group.Key.Currency, group.Sum(t => t.Amount), group.Count()))
            .ToListAsync(cancellationToken);

        var byCentre = await transactions
            .GroupBy(t => new { Key = t.CentreCode, t.Currency })
            .Select(group => new RevenueGroup(group.Key.Key, group.Key.Currency, group.Sum(t => t.Amount), group.Count()))
            .ToListAsync(cancellationToken);

        var byVisaType = await transactions
            .GroupBy(t => new { Key = t.VisaTypeCode, t.Currency })
            .Select(group => new RevenueGroup(group.Key.Key, group.Key.Currency, group.Sum(t => t.Amount), group.Count()))
            .ToListAsync(cancellationToken);

        var byService = await transactions
            .GroupBy(t => new { Key = t.ServiceName, t.Currency })
            .Select(group => new RevenueGroup(group.Key.Key, group.Key.Currency, group.Sum(t => t.Amount), group.Count()))
            .ToListAsync(cancellationToken);

        var days = await transactions
            .GroupBy(t => new { t.TransactionDate, t.Currency })
            .Select(group => new DailyRevenue(
                group.Key.TransactionDate, group.Key.Currency, group.Sum(t => t.Amount), group.Count()))
            .ToListAsync(cancellationToken);

        return new VasRevenueDto(
            context.Meta,
            [
                .. kpiCards.CreateRevenueCards(now, before),
                kpiCards.Create(
                    KpiKeys.RevenueTransactions,
                    KpiUnit.Count,
                    now.Sum(total => total.Transactions),
                    before?.Sum(total => total.Transactions)),
            ],
            BuildTrend(scope.Current, days),
            BuildBreakdown(byCountry, await db.CountryNamesAsync(cancellationToken)),
            BuildBreakdown(byCentre, await db.CentreNamesAsync(cancellationToken)),
            BuildBreakdown(byVisaType, await db.VisaTypeNamesAsync(cancellationToken)),
            BuildBreakdown(byService, names: null));
    }

    private static RevenueTrendDto BuildTrend(DateRange range, List<DailyRevenue> days)
    {
        var currencies = days.Select(day => day.Currency).Distinct().Order(StringComparer.Ordinal).ToList();

        var points = TrendSeries
            .Build(
                range,
                days,
                day => day.Day,
                (period, rows) => currencies.Select(currency =>
                {
                    var inCurrency = rows.Where(row => row.Currency == currency).ToList();
                    return new RevenueTrendPointDto(
                        period,
                        currency,
                        Math.Round(inCurrency.Sum(row => row.Amount), 2),
                        inCurrency.Sum(row => row.Transactions));
                }))
            .SelectMany(periodPoints => periodPoints)
            .ToList();

        return new RevenueTrendDto(TrendSeries.GranularityFor(range), points);
    }

    private static List<RevenueBreakdownRowDto> BuildBreakdown(
        List<RevenueGroup> groups,
        IReadOnlyDictionary<string, NamedItem>? names)
    {
        var totals = groups
            .GroupBy(group => group.Currency)
            .ToDictionary(currency => currency.Key, currency => currency.Sum(group => group.Amount));

        return groups
            .OrderBy(group => group.Currency, StringComparer.Ordinal)
            .ThenByDescending(group => group.Amount)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new RevenueBreakdownRowDto(
                group.Key,
                names?.GetValueOrDefault(group.Key)?.Name ?? group.Key,
                group.Currency,
                Math.Round(group.Amount, 2),
                group.Transactions,
                KpiMath.Percent(group.Amount, totals[group.Currency])))
            .ToList();
    }
}
