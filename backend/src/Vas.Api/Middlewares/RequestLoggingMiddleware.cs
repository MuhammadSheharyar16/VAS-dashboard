using System.Diagnostics;
using Vas.Infrastructure.Identity;

namespace Vas.Api.Middlewares;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    private const string AnonymousUser = "anonymous";

    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            // The Entra object ID is the access trail; names, tokens and bodies are never logged.
            logger.LogInformation(
                "HTTP {Method} {Path}{Query} responded {StatusCode} in {ElapsedMs} ms for {UserId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Request.QueryString.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                EntraClaims.ObjectId(context.User) ?? AnonymousUser);
        }
    }
}
