using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Application.Common.Reporting;

public interface IReportContextService
{
    /// <summary>Applies the default view, checks the codes against the lookups and reads the data freshness.</summary>
    Task<ReportContext> ResolveAsync(ReportFilter filter, CancellationToken cancellationToken);
}

public sealed class ReportContextService(
    IReportingDbContext db,
    IDateTimeProvider clock,
    IOptions<KpiThresholdOptions> thresholds) : IReportContextService
{
    public async Task<ReportContext> ResolveAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        var lastRefreshedAtUtc = await GetLastSuccessfulRefreshAsync(cancellationToken);
        var current = ResolveRange(filter, lastRefreshedAtUtc);

        await EnsureCodesExistAsync(filter, cancellationToken);

        var previous = filter.Compare == ComparisonPeriod.PreviousPeriod ? current.Previous() : null;
        var scope = new ReportScope(current, previous, filter.CountryCode, filter.CentreCode, filter.VisaTypeCode);

        var meta = new ReportMetaDto(
            clock.UtcNow,
            db.DataSource,
            new DataFreshnessDto(lastRefreshedAtUtc),
            new AppliedFilterDto(
                current.From,
                current.ToExclusive,
                filter.CountryCode,
                filter.CentreCode,
                filter.VisaTypeCode,
                filter.Compare,
                previous?.From,
                previous?.ToExclusive),
            thresholds.Value.ArePlaceholders);

        return new ReportContext(scope, meta);
    }

    private async Task<DateTime?> GetLastSuccessfulRefreshAsync(CancellationToken cancellationToken)
    {
        var latest = await db.RefreshLogs
            .Where(log => log.Status == RefreshStatus.Succeeded)
            .OrderByDescending(log => log.RefreshedAtUtc)
            .Select(log => log.RefreshedAtUtc)
            .Take(1)
            .ToListAsync(cancellationToken);

        return latest.Count == 0 ? null : DateTime.SpecifyKind(latest[0], DateTimeKind.Utc);
    }

    /// <summary>
    /// The default view is the last 30 complete days of data: it ends at the day of the last
    /// successful refresh, so the newest day shown is one the Gold layer has fully loaded.
    /// </summary>
    private DateRange ResolveRange(ReportFilter filter, DateTime? lastRefreshedAtUtc)
    {
        var defaultEnd = lastRefreshedAtUtc is null
            ? DateOnly.FromDateTime(clock.UtcNow).AddDays(1)
            : DateOnly.FromDateTime(lastRefreshedAtUtc.Value);

        if (filter.LastDays is { } lastDays)
        {
            return new DateRange(defaultEnd.AddDays(-lastDays), defaultEnd);
        }

        var toExclusive = filter.ToExclusive ?? (filter.From is null ? defaultEnd : filter.From.Value.AddDays(DateRange.DefaultDays));
        var from = filter.From ?? toExclusive.AddDays(-DateRange.DefaultDays);

        var problem = DateRange.Validate(from, toExclusive);
        if (problem is not null)
        {
            throw new FluentValidation.ValidationException([new ValidationFailure(nameof(ReportFilter.From), problem)]);
        }

        return new DateRange(from, toExclusive);
    }

    private async Task EnsureCodesExistAsync(ReportFilter filter, CancellationToken cancellationToken)
    {
        var failures = new List<ValidationFailure>();

        if (filter.CountryCode is { } countryCode)
        {
            var found = await db.Countries
                .Where(country => country.CountryCode == countryCode)
                .Select(country => country.CountryCode)
                .ToListAsync(cancellationToken);
            AddIfMissing(failures, found, nameof(ReportFilter.CountryCode), "Unknown country.");
        }

        if (filter.CentreCode is { } centreCode)
        {
            var found = await db.Centres
                .Where(centre => centre.CentreCode == centreCode)
                .Select(centre => centre.CountryCode)
                .ToListAsync(cancellationToken);
            AddIfMissing(failures, found, nameof(ReportFilter.CentreCode), "Unknown centre.");

            // A centre belongs to one country; the two together must agree or the report is silently empty.
            if (filter.CountryCode is { } selectedCountry
                && found.Count > 0
                && !found.Contains(selectedCountry, StringComparer.OrdinalIgnoreCase))
            {
                failures.Add(new ValidationFailure(
                    nameof(ReportFilter.CentreCode), "The centre does not belong to the selected country."));
            }
        }

        if (filter.VisaTypeCode is { } visaTypeCode)
        {
            var found = await db.VisaTypes
                .Where(visaType => visaType.VisaTypeCode == visaTypeCode)
                .Select(visaType => visaType.VisaTypeCode)
                .ToListAsync(cancellationToken);
            AddIfMissing(failures, found, nameof(ReportFilter.VisaTypeCode), "Unknown visa type.");
        }

        if (failures.Count > 0)
        {
            throw new FluentValidation.ValidationException(failures);
        }
    }

    private static void AddIfMissing(List<ValidationFailure> failures, List<string> found, string field, string message)
    {
        if (found.Count == 0)
        {
            failures.Add(new ValidationFailure(field, message));
        }
    }
}
