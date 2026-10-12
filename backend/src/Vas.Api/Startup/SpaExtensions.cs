using Vas.Shared.Results;

namespace Vas.Api.Startup;

public static class SpaExtensions
{
    private const string IndexFile = "index.html";

    public static WebApplication UseSpaStaticFiles(this WebApplication app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();

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
            app.MapFallbackToFile(IndexFile);
        }

        return app;
    }
}
