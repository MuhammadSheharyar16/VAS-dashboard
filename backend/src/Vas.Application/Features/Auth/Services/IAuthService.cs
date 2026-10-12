using Vas.Application.Features.Auth.DTOs;

namespace Vas.Application.Features.Auth.Services;

public interface IAuthService
{
    CurrentUserDto GetCurrentUser();

    IReadOnlyList<PermissionDto> GetPermissions();

    IReadOnlyList<MenuItemDto> GetMenu();

    ClientConfigDto GetClientConfig();
}
