using Vas.Domain.Enums;
using Vas.Shared.Constants;

namespace Vas.Application.Features.Auth.Rules;

public sealed record ScreenPermission(string Screen, PermissionAction Action);

/// <summary>
/// The single place that says what each Entra app role may do. There is no permission database.
/// </summary>
public static class RolePermissionMap
{
    private static readonly Dictionary<string, ScreenPermission[]> Grants = new(StringComparer.Ordinal)
    {
        [AppRoles.Viewer] =
        [
            new(Screens.Dashboard, PermissionAction.View),
            new(Screens.ExecutiveSummary, PermissionAction.View),
            new(Screens.Vas, PermissionAction.View),
        ],
    };

    public static IReadOnlyList<ScreenPermission> ForRoles(IEnumerable<string> roles) =>
        roles
            .SelectMany(role => Grants.TryGetValue(role, out var permissions) ? permissions : [])
            .Distinct()
            .ToList();

    public static bool IsGranted(IEnumerable<string> roles, string screen, PermissionAction action) =>
        ForRoles(roles).Contains(new ScreenPermission(screen, action));
}
