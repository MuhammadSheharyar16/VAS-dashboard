using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Vas.Application.Common.Options;
using Vas.Shared.Results;

namespace Vas.Api.Startup;

public static class RateLimitingExtensions
{
    public const string ApiPolicy = "api";

    private const string ObjectIdClaim = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RateLimitingOptions>()
            .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = async (rejection, cancellationToken) =>
                await rejection.HttpContext.Response.WriteAsJsonAsync(
                    ApiResponse.Fail(
                        "Too many requests. Try again shortly.",
                        traceId: rejection.HttpContext.TraceIdentifier),
                    cancellationToken);

            limiter.AddPolicy(ApiPolicy, context =>
            {
                var options = context.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext context) =>
        context.User.FindFirst(ObjectIdClaim)?.Value
        ?? context.User.FindFirst("oid")?.Value
        ?? context.Connection.RemoteIpAddress?.ToString()
        ?? "anonymous";
}
