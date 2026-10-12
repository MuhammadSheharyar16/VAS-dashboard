using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Vas.Api.Startup;
using Vas.Shared.Results;

namespace Vas.ApiTests;

public class InvalidModelStateResponseTests
{
    [Fact]
    public void Binding_failure_returns_400_in_the_envelope_with_field_errors()
    {
        var context = new ActionContext(
            new DefaultHttpContext { TraceIdentifier = "trace-7" },
            new RouteData(),
            new ActionDescriptor());
        context.ModelState.AddModelError("PageSize", "The value 'abc' is not valid.");

        var result = ApiBehaviorExtensions.CreateInvalidModelStateResponse(context);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var body = Assert.IsType<ApiResponse<object>>(badRequest.Value);
        Assert.False(body.Success);
        Assert.Equal("trace-7", body.TraceId);
        var error = Assert.Single(body.Errors);
        Assert.Equal("pageSize", error.Field);
    }
}
