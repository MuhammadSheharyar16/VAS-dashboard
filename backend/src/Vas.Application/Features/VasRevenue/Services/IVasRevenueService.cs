using Vas.Application.Features.VasRevenue.DTOs;

namespace Vas.Application.Features.VasRevenue.Services;

public interface IVasRevenueService
{
    Task<VasRevenueDto> GetAsync(VasRevenueQueryParams query, CancellationToken cancellationToken);
}
