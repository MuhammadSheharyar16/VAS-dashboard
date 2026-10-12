using Vas.Shared.Results;

namespace Vas.Api.Startup;

public static class ErrorResponseExtensions
{
    /// <summary>
    /// Gives body-less API failures (the 401 challenge, the 403 forbid) the standard envelope.
    /// </summary>
    public static WebApplication UseApiStatusCodeEnvelope(this WebApplication app)
    {
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var context = statusCodeContext.HttpContext;
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                return;
            }

            var message = context.Response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => "Authentication is required.",
                StatusCodes.Status403Forbidden => "You do not have access to this resource.",
                StatusCodes.Status404NotFound => "Resource not found.",
                _ => "The request could not be completed.",
            };

            await context.Response.WriteAsJsonAsync(
                ApiResponse.Fail(message, traceId: context.TraceIdentifier));
        });

        return app;
    }
}
