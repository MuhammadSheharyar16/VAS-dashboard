using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Authorization;
using Vas.Domain.Enums;

namespace Vas.Infrastructure.Authorization;

public sealed record PermissionRequirement(string Screen, PermissionAction Action) : IAuthorizationRequirement
{
    private const string Prefix = "Permission:";

    public string PolicyName => $"{Prefix}{Screen}:{Action}";

    public static bool TryParse(string policyName, [NotNullWhen(true)] out PermissionRequirement? requirement)
    {
        requirement = null;

        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = policyName[Prefix.Length..].Split(':');
        if (parts.Length != 2 || parts[0].Length == 0 || !Enum.TryParse<PermissionAction>(parts[1], out var action))
        {
            return false;
        }

        requirement = new PermissionRequirement(parts[0], action);
        return true;
    }
}
