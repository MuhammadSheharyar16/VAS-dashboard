namespace Vas.Api.Startup;

public static class RequestLimitExtensions
{
    /// <summary>The API only answers GET requests, so nothing legitimate sends a large body.</summary>
    public const long MaxRequestBodyBytes = 64 * 1024;

    public static WebApplicationBuilder AddApiRequestLimits(this WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = MaxRequestBodyBytes);

        return builder;
    }
}
