using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Vas.Infrastructure.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermissionRequirement.TryParse(policyName, out var requirement))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
