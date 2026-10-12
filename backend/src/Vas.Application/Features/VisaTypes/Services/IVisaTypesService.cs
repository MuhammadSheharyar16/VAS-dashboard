using Vas.Application.Features.VisaTypes.DTOs;

namespace Vas.Application.Features.VisaTypes.Services;

public interface IVisaTypesService
{
    Task<VisaTypesDto> GetAsync(VisaTypesQueryParams query, CancellationToken cancellationToken);
}
