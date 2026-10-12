using Microsoft.AspNetCore.Authorization;
using Vas.Domain.Enums;

namespace Vas.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string screen, PermissionAction action)
    {
        Policy = new PermissionRequirement(screen, action).PolicyName;
    }
}
