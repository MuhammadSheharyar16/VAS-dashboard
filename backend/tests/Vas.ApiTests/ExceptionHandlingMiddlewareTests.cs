using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Vas.Api.Middlewares;
using Vas.Application.Common.Exceptions;
using Vas.Shared.Results;

namespace Vas.ApiTests;

public class ExceptionHandlingMiddlewareTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static TheoryData<Exception, int> Mappings => new()
    {
        { new FluentValidation.ValidationException([new ValidationFailure("From", "From is required.")]), 400 },
        { new UnauthenticatedException(), 401 },
        { new ForbiddenException(), 403 },
        { new NotFoundException("Centre not found."), 404 },
        { new BusinessRuleException("Date range is too long."), 422 },
        { new InvalidOperationException("boom"), 500 },
        { new UnauthorizedAccessException("disk"), 500 },
    };

    [Theory]
    [MemberData(nameof(Mappings))]
    public async Task Exception_maps_to_its_status_code_in_the_envelope(Exception exception, int expectedStatus)
    {
        var (context, body) = await InvokeAsync(exception);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.False(body.Success);
        Assert.Equal("trace-42", body.TraceId);
    }

    [Fact]
    public async Task Validation_failure_returns_field_errors_with_camel_case_names()
    {
        var exception = new FluentValidation.ValidationException(
            [new ValidationFailure("CountryCode", "Country code is not valid.")]);

        var (_, body) = await InvokeAsync(exception);

        var error = Assert.Single(body.Errors);
        Assert.Equal("countryCode", error.Field);
        Assert.Equal("Country code is not valid.", error.Message);
    }

    [Fact]
    public async Task Unhandled_exception_does_not_leak_its_message()
    {
        var (_, body) = await InvokeAsync(new InvalidOperationException("SELECT * FROM gold.secret"));

        Assert.DoesNotContain("SELECT", body.Message);
        Assert.Empty(body.Errors);
    }

    private static async Task<(HttpContext Context, ApiResponse<object> Body)> InvokeAsync(Exception exception)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
            TraceIdentifier = "trace-42",
        };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<ApiResponse<object>>(context.Response.Body, Json);

        return (context, body!);
    }
}
