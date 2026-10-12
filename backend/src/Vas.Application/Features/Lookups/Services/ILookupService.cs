using Vas.Application.Features.Lookups.DTOs;

namespace Vas.Application.Features.Lookups.Services;

public interface ILookupService
{
    Task<IReadOnlyList<LookupItemDto>> GetCountriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<LookupItemDto>> GetCentresAsync(CentreLookupQueryParams query, CancellationToken cancellationToken);

    Task<IReadOnlyList<LookupItemDto>> GetVisaTypesAsync(CancellationToken cancellationToken);
}
