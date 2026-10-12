using Microsoft.AspNetCore.Mvc;
using Vas.Shared.Errors;
using Vas.Shared.Results;

namespace Vas.Api.Startup;

public static class ApiBehaviorExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
                options.InvalidModelStateResponseFactory = CreateInvalidModelStateResponse);

        return services;
    }

    public static IActionResult CreateInvalidModelStateResponse(ActionContext context)
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .SelectMany(entry => entry.Value!.Errors.Select(error => new ErrorDetail(
                ToCamelCase(entry.Key),
                string.IsNullOrEmpty(error.ErrorMessage) ? "The value is not valid." : error.ErrorMessage)))
            .ToList();

        return new BadRequestObjectResult(
            ApiResponse.Fail("Validation failed.", errors, context.HttpContext.TraceIdentifier));
    }

    private static string ToCamelCase(string name) =>
        string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
