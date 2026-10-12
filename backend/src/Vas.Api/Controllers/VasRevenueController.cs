using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.VasRevenue.DTOs;
using Vas.Application.Features.VasRevenue.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/vas/revenue")]
[Tags("VAS")]
[RequirePermission(Screens.Vas, PermissionAction.View)]
public sealed class VasRevenueController(IVasRevenueService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VasRevenueDto>>> Get(
        [FromQuery] VasRevenueQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetAsync(query, cancellationToken)));
}
