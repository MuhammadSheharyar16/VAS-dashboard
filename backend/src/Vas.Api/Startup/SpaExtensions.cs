using Vas.Application.Features.Auth.Services;
using Vas.Shared.Results;

namespace Vas.Api.Startup;

public static class SpaExtensions
{
    public const string ClientConfigPath = "/config.json";

    private const string IndexFile = "index.html";

    public static WebApplication UseSpaStaticFiles(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();

        return app;
    }

    /// <summary>
    /// Serves the non-secret Entra settings at runtime so one image works in every environment.
    /// </summary>
    public static WebApplication MapClientConfig(this WebApplication app)
    {
        app.MapGet(ClientConfigPath, (IAuthService authService, HttpContext context) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                return Results.Json(authService.GetClientConfig());
            })
            .AllowAnonymous()
            .ExcludeFromDescription();

        return app;
    }

    public static WebApplication MapSpaFallback(this WebApplication app)
    {
        // An unknown API route must answer with the envelope, never with the SPA page.
        app.MapFallback("/api/{**path}", (HttpContext context) => Results.Json(
                ApiResponse.Fail("Resource not found.", traceId: context.TraceIdentifier),
                statusCode: StatusCodes.Status404NotFound))
            .RequireRateLimiting(RateLimitingExtensions.ApiPolicy);

        var webRoot = app.Environment.WebRootPath;
        if (!string.IsNullOrEmpty(webRoot) && File.Exists(Path.Combine(webRoot, IndexFile)))
        {
            app.MapFallbackToFile(IndexFile).AllowAnonymous();
        }

        return app;
    }
}
