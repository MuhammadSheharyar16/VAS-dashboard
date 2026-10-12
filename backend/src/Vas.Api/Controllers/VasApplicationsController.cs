using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.VasApplications.DTOs;
using Vas.Application.Features.VasApplications.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/vas/applications")]
[Tags("VAS")]
[RequirePermission(Screens.Vas, PermissionAction.View)]
public sealed class VasApplicationsController(IVasApplicationsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VasApplicationsDto>>> Get(
        [FromQuery] VasApplicationsQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetAsync(query, cancellationToken)));
}
