using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Vas.Application.Common.Options;

namespace Vas.Api.Startup;

public static class AuthenticationExtensions
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<EntraOptions>()
            .Bind(configuration.GetSection(EntraOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(
                options => environment.IsDevelopment() || !options.HasPlaceholderIds(),
                "AzureAd TenantId, ClientId and SpaClientId must be set to real values outside Development.")
            .ValidateOnStart();

        services.AddMicrosoftIdentityWebApiAuthentication(configuration, EntraOptions.SectionName);

        services.Configure<JwtBearerOptions>(
            JwtBearerDefaults.AuthenticationScheme,
            options => options.TokenValidationParameters.ClockSkew = ClockSkew);

        // Every endpoint needs a signed-in user unless it opts out with AllowAnonymous.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }
}
