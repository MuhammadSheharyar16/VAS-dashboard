using Microsoft.Extensions.Options;
using Vas.Application.Common.Exceptions;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Application.Features.Auth.DTOs;
using Vas.Application.Features.Auth.Rules;
using Vas.Domain.Enums;

namespace Vas.Application.Features.Auth.Services;

public sealed class AuthService(ICurrentUser currentUser, IOptions<EntraOptions> entraOptions) : IAuthService
{
    public CurrentUserDto GetCurrentUser()
    {
        EnsureAuthenticated();

        return new CurrentUserDto(
            currentUser.UserId,
            currentUser.DisplayName,
            currentUser.Username,
            currentUser.Roles);
    }

    public IReadOnlyList<PermissionDto> GetPermissions()
    {
        EnsureAuthenticated();

        return RolePermissionMap.ForRoles(currentUser.Roles)
            .GroupBy(permission => permission.Screen, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PermissionDto(
                group.Key,
                group.Select(permission => permission.Action.ToString()).Order(StringComparer.Ordinal).ToList()))
            .ToList();
    }

    public IReadOnlyList<MenuItemDto> GetMenu()
    {
        EnsureAuthenticated();

        return ScreenRegistry.MenuScreens
            .Where(screen => screen.IsComingSoon
                || RolePermissionMap.IsGranted(currentUser.Roles, screen.Code, PermissionAction.View))
            .OrderBy(screen => screen.Order)
            .Select(screen => new MenuItemDto(
                screen.Code, screen.Title, screen.Route, screen.Order, screen.IsComingSoon))
            .ToList();
    }

    public ClientConfigDto GetClientConfig()
    {
        var entra = entraOptions.Value;

        return new ClientConfigDto(entra.TenantId, entra.SpaClientId, entra.Authority, entra.ApiScope);
    }

    private void EnsureAuthenticated()
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthenticatedException();
        }
    }
}
