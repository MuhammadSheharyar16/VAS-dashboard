using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.VisaTypes.DTOs;
using Vas.Application.Features.VisaTypes.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/vas/visa-types")]
[Tags("VAS")]
[RequirePermission(Screens.Vas, PermissionAction.View)]
public sealed class VisaTypesController(IVisaTypesService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<VisaTypesDto>>> Get(
        [FromQuery] VisaTypesQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetAsync(query, cancellationToken)));
}
