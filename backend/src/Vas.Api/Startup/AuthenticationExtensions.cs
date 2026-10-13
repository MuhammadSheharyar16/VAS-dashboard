using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Vas.Application.Common.Options;
using Vas.Infrastructure.Identity;

namespace Vas.Api.Startup;

public static class AuthenticationExtensions
{
    private static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EntraOptions>()
            .Bind(configuration.GetSection(EntraOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate<IHostEnvironment>(
                (options, environment) => environment.IsDevelopment() || !options.HasPlaceholderIds(),
                "AzureAd ClientId and SpaClientId must be set to real values outside Development, and TenantId must not be the placeholder.")
            .Validate(
                options => options.HasValidAllowedTenantIds(),
                "AzureAd AllowedTenantIds must be comma-separated tenant IDs (GUIDs).")
            .ValidateOnStart();

        services.AddMicrosoftIdentityWebApiAuthentication(configuration, EntraOptions.SectionName);

        // Unless exactly one tenant is allowed, issuers are validated against the multi-tenant endpoint:
        // any Entra organisation's issuer passes there, and a configured tenant list narrows it again below.
        services.AddOptions<MicrosoftIdentityOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<EntraOptions>>((identity, entra) => identity.TenantId = entra.Value.AuthorityTenant);

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<EntraOptions>>((options, entra) =>
            {
                options.TokenValidationParameters.ClockSkew = ClockSkew;
                RejectOtherTenants(options, entra.Value);
            });

        // Every endpoint needs a signed-in user unless it opts out with AllowAnonymous.
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        return services;
    }

    /// <summary>
    /// When tenants are configured, a valid token from any other tenant is refused like an invalid token (401).
    /// With none configured, the token's own organisation is accepted and the scope and role decide access.
    /// </summary>
    private static void RejectOtherTenants(JwtBearerOptions options, EntraOptions entra)
    {
        var allowedTenants = entra.AllowedTenants.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (allowedTenants.Count == 0)
        {
            return;
        }

        options.Events ??= new JwtBearerEvents();
        var next = options.Events.OnTokenValidated;

        options.Events.OnTokenValidated = async context =>
        {
            var tenantId = context.Principal is null ? null : EntraClaims.TenantId(context.Principal);
            if (tenantId is null || !allowedTenants.Contains(tenantId))
            {
                context.Fail("The token's tenant is not allowed.");
                return;
            }

            await next(context);
        };
    }
}
