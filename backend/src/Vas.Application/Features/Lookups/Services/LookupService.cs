using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.Lookups.DTOs;

namespace Vas.Application.Features.Lookups.Services;

/// <summary>The choices for the filter dropdowns: small lists, sorted by name and capped.</summary>
public sealed class LookupService(
    IReportingDbContext db,
    IValidator<CentreLookupQueryParams> centreValidator) : ILookupService
{
    public async Task<IReadOnlyList<LookupItemDto>> GetCountriesAsync(CancellationToken cancellationToken) =>
        await db.Countries
            .OrderBy(country => country.CountryName)
            .ThenBy(country => country.CountryCode)
            .Take(ReportLookups.MaxItems)
            .Select(country => new LookupItemDto(country.CountryCode, country.CountryName, country.Region))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LookupItemDto>> GetCentresAsync(
        CentreLookupQueryParams query, CancellationToken cancellationToken)
    {
        await centreValidator.ValidateAndThrowAsync(query, cancellationToken);

        var centres = db.Centres.AsQueryable();
        if (query.CountryCode is { } countryCode)
        {
            centres = centres.Where(centre => centre.CountryCode == countryCode);
        }

        return await centres
            .OrderBy(centre => centre.CentreName)
            .ThenBy(centre => centre.CentreCode)
            .Take(ReportLookups.MaxItems)
            .Select(centre => new LookupItemDto(centre.CentreCode, centre.CentreName, centre.CountryCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetVisaTypesAsync(CancellationToken cancellationToken) =>
        await db.VisaTypes
            .OrderBy(visaType => visaType.VisaTypeName)
            .ThenBy(visaType => visaType.VisaTypeCode)
            .Take(ReportLookups.MaxItems)
            .Select(visaType => new LookupItemDto(visaType.VisaTypeCode, visaType.VisaTypeName, visaType.Category))
            .ToListAsync(cancellationToken);
}
