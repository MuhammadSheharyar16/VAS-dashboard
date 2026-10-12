using Vas.Shared.Constants;

namespace Vas.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Resolve(context.Request.Headers[HttpHeaders.CorrelationId].ToString());
        context.TraceIdentifier = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HttpHeaders.CorrelationId] = correlationId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static string Resolve(string incoming)
    {
        var isSafe = incoming.Length is > 0 and <= MaxLength
            && incoming.All(character => char.IsAsciiLetterOrDigit(character) || character == '-');

        return isSafe ? incoming : Guid.NewGuid().ToString("N");
    }
}
