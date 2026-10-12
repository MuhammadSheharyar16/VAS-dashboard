using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.VasProcessing.DTOs;
using Vas.Application.Features.VasProcessing.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/vas/processing")]
[Tags("VAS")]
[RequirePermission(Screens.Vas, PermissionAction.View)]
public sealed class VasProcessingController(IVasProcessingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VasProcessingDto>>> Get(
        [FromQuery] VasProcessingQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetAsync(query, cancellationToken)));
}
