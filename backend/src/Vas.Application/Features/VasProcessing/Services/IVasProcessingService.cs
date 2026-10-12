using Vas.Application.Features.VasProcessing.DTOs;

namespace Vas.Application.Features.VasProcessing.Services;

public interface IVasProcessingService
{
    Task<VasProcessingDto> GetAsync(VasProcessingQueryParams query, CancellationToken cancellationToken);
}
