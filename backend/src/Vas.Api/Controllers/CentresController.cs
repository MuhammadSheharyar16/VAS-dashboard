using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.Centres.DTOs;
using Vas.Application.Features.Centres.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/vas/centres")]
[Tags("VAS")]
[RequirePermission(Screens.Vas, PermissionAction.View)]
public sealed class CentresController(ICentresService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<CentresDto>>> Get(
        [FromQuery] CentresQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetAsync(query, cancellationToken)));
}
