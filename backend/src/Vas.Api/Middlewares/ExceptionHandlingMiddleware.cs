using System.Data.Common;
using Azure.Identity;
using Vas.Application.Common.Exceptions;
using Vas.Shared.Errors;
using Vas.Shared.Results;

namespace Vas.Api.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";
    private const string DataSourceUnavailableMessage = "The reporting data is temporarily unavailable. Try again shortly.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer.
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, message, errors) = Map(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError(exception, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path.Value);
            }
            else
            {
                logger.LogWarning("Request {Method} {Path} failed with {StatusCode} ({ExceptionType})",
                    context.Request.Method, context.Request.Path.Value, statusCode, exception.GetType().Name);
            }

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(
                ApiResponse.Fail(message, errors, context.TraceIdentifier));
        }
    }

    private static (int StatusCode, string Message, IReadOnlyList<ErrorDetail> Errors) Map(Exception exception) =>
        exception switch
        {
            FluentValidation.ValidationException validation => (
                StatusCodes.Status400BadRequest,
                "Validation failed.",
                validation.Errors
                    .Select(failure => new ErrorDetail(ToCamelCase(failure.PropertyName), failure.ErrorMessage))
                    .ToList()),
            UnauthenticatedException => (StatusCodes.Status401Unauthorized, exception.Message, []),
            ForbiddenException => (StatusCodes.Status403Forbidden, exception.Message, []),
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message, []),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, exception.Message, []),
            _ when IsDataSourceFailure(exception) => (
                StatusCodes.Status503ServiceUnavailable, DataSourceUnavailableMessage, []),
            _ => (StatusCodes.Status500InternalServerError, UnexpectedErrorMessage, []),
        };

    /// <summary>
    /// True when the reporting source could not be reached or queried: a SQL error, or no token for it.
    /// The retry strategy wraps the cause, so the whole chain is checked.
    /// </summary>
    private static bool IsDataSourceFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or AuthenticationFailedException)
            {
                return true;
            }
        }

        return false;
    }

    private static string? ToCamelCase(string? name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
