using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.Application.Features.ExecutiveSummary.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/executive-summary")]
[Tags("Executive Summary")]
[RequirePermission(Screens.ExecutiveSummary, PermissionAction.View)]
public sealed class ExecutiveSummaryController(IExecutiveSummaryService executiveSummaryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<ExecutiveSummaryDto>>> Get(
        [FromQuery] ExecutiveSummaryQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await executiveSummaryService.GetAsync(query, cancellationToken)));
}
