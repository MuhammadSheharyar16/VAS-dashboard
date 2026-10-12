using Microsoft.AspNetCore.Mvc;
using Vas.Application.Features.Auth.DTOs;
using Vas.Application.Features.Auth.Services;
using Vas.Domain.Enums;
using Vas.Infrastructure.Authorization;
using Vas.Shared.Constants;
using Vas.Shared.Results;

namespace Vas.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Tags("Auth")]
[RequirePermission(Screens.Dashboard, PermissionAction.View)]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<ApiResponse<CurrentUserDto>> GetCurrentUser() =>
        Ok(ApiResponse.Ok(authService.GetCurrentUser()));

    [HttpGet("me/permissions")]
    public ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>> GetPermissions() =>
        Ok(ApiResponse.Ok(authService.GetPermissions()));

    [HttpGet("me/menu")]
    public ActionResult<ApiResponse<IReadOnlyList<MenuItemDto>>> GetMenu() =>
        Ok(ApiResponse.Ok(authService.GetMenu()));
}
