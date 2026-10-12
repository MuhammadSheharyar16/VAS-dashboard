using Vas.Application.Features.Centres.DTOs;

namespace Vas.Application.Features.Centres.Services;

public interface ICentresService
{
    Task<CentresDto> GetAsync(CentresQueryParams query, CancellationToken cancellationToken);
}
