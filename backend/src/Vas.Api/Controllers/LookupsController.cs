using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.Lookups.DTOs;
using Vas.Application.Features.Lookups.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/lookups")]
[Tags("Lookups")]
[RequirePermission(Screens.Dashboard, PermissionAction.View)]
public sealed class LookupsController(ILookupService service) : ControllerBase
{
    [HttpGet("countries")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> GetCountries(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetCountriesAsync(cancellationToken)));

    [HttpGet("centres")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> GetCentres(
        [FromQuery] CentreLookupQueryParams query,
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetCentresAsync(query, cancellationToken)));

    [HttpGet("visa-types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LookupItemDto>>>> GetVisaTypes(
        CancellationToken cancellationToken) =>
        Ok(ApiResponse.Ok(await service.GetVisaTypesAsync(cancellationToken)));
}
