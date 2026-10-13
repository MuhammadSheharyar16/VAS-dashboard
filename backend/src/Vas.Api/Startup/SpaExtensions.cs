using Vas.Application.Features.Auth.Services;
using Vas.Shared.Results;

namespace Vas.Api.Startup;

public static class SpaExtensions
{
    public const string ClientConfigPath = "/config.json";

    public const string NoWebAppMessage =
        "The AEGI VAS Dashboard API is running. The web app is not built into this image yet. "
        + "Check /healthz for health. The API under /api/v1 needs a Microsoft Entra ID access token.";

    private const string IndexFile = "index.html";

    private const string HashedAssetsPath = "/assets";

    // The page names its scripts by content hash. A browser that keeps an old page keeps asking for
    // scripts the next image no longer has, so the page is revalidated on every load and only the
    // hashed files are cached for good.
    private static readonly StaticFileOptions SpaFileOptions = new()
    {
        OnPrepareResponse = context =>
        {
            var isHashedAsset = context.Context.Request.Path.StartsWithSegments(
                HashedAssetsPath, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(context.File.Name, IndexFile, StringComparison.OrdinalIgnoreCase);

            context.Context.Response.Headers.CacheControl = isHashedAsset
                ? "public, max-age=31536000, immutable"
                : "no-cache";
        },
    };

    public static WebApplication UseSpaStaticFiles(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles(SpaFileOptions);

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
            app.MapFallbackToFile(IndexFile, SpaFileOptions).AllowAnonymous();
        }
        else
        {
            // Without the web app built in, say so at the root instead of answering with a bare 401.
            app.MapGet("/", () => Results.Text(NoWebAppMessage)).AllowAnonymous().ExcludeFromDescription();
        }

        return app;
    }
}
