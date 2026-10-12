using Vas.Application.Features.VasApplications.DTOs;

namespace Vas.Application.Features.VasApplications.Services;

public interface IVasApplicationsService
{
    Task<VasApplicationsDto> GetAsync(VasApplicationsQueryParams query, CancellationToken cancellationToken);
}
