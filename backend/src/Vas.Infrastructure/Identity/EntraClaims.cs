using System.Security.Claims;
using Vas.Shared.Constants;

namespace Vas.Infrastructure.Identity;

public static class EntraClaims
{
    public static string? ObjectId(ClaimsPrincipal user) =>
        First(user, EntraClaimTypes.ObjectId, EntraClaimTypes.ObjectIdUri);

    public static string? TenantId(ClaimsPrincipal user) =>
        First(user, EntraClaimTypes.TenantId, EntraClaimTypes.TenantIdUri);

    public static string? DisplayName(ClaimsPrincipal user) =>
        First(user, EntraClaimTypes.Name, ClaimTypes.Name);

    public static string? Username(ClaimsPrincipal user) =>
        First(user, EntraClaimTypes.PreferredUsername, ClaimTypes.Upn);

    public static IReadOnlyCollection<string> Roles(ClaimsPrincipal user) =>
        user.FindAll(claim => claim.Type is EntraClaimTypes.Roles or ClaimTypes.Role)
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static IReadOnlyCollection<string> Scopes(ClaimsPrincipal user) =>
        user.FindAll(claim => claim.Type is EntraClaimTypes.Scope or EntraClaimTypes.ScopeUri)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string? First(ClaimsPrincipal user, string shortType, string mappedType) =>
        user.FindFirst(shortType)?.Value ?? user.FindFirst(mappedType)?.Value;
}
