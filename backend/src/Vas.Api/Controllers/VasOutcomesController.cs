using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.VasOutcomes.DTOs;
using Vas.Application.Features.VasOutcomes.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/vas/outcomes")]
[Tags("VAS")]
[RequirePermission(Screens.Vas, PermissionAction.View)]
public sealed class VasOutcomesController(IVasOutcomesService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VasOutcomesDto>>> Get(
        [FromQuery] VasOutcomesQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetAsync(query, cancellationToken)));
}
