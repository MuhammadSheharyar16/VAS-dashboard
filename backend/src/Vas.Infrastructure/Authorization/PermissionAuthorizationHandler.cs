using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Vas.Application.Common.Options;
using Vas.Application.Features.Auth.Rules;
using Vas.Infrastructure.Identity;

namespace Vas.Infrastructure.Authorization;

/// <summary>
/// Permissions come from the token's role claims, so a role change applies at the user's next token.
/// </summary>
public sealed class PermissionAuthorizationHandler(IOptions<EntraOptions> entraOptions)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;

        var hasScope = EntraClaims.Scopes(user).Contains(entraOptions.Value.RequiredScope, StringComparer.Ordinal);
        if (user.Identity?.IsAuthenticated == true
            && hasScope
            && RolePermissionMap.IsGranted(EntraClaims.Roles(user), requirement.Screen, requirement.Action))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
