using Vas.Application.Features.VasOutcomes.DTOs;

namespace Vas.Application.Features.VasOutcomes.Services;

public interface IVasOutcomesService
{
    Task<VasOutcomesDto> GetAsync(VasOutcomesQueryParams query, CancellationToken cancellationToken);
}
