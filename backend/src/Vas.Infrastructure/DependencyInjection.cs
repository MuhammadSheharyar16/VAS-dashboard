using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Vas.Application.Common.Interfaces;
using Vas.Infrastructure.Authorization;
using Vas.Infrastructure.Common;
using Vas.Infrastructure.Identity;

namespace Vas.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
