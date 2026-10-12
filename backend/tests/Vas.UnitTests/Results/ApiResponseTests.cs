using Vas.Shared.Errors;
using Vas.Shared.Results;

namespace Vas.UnitTests.Results;

public class ApiResponseTests
{
    [Fact]
    public void Ok_wraps_the_data_with_success_and_no_errors()
    {
        var response = ApiResponse.Ok(42);

        Assert.True(response.Success);
        Assert.Equal(42, response.Data);
        Assert.Empty(response.Errors);
        Assert.Null(response.TraceId);
    }

    [Fact]
    public void Fail_carries_the_errors_and_trace_id_and_no_data()
    {
        var errors = new[] { new ErrorDetail("from", "From is required.") };

        var response = ApiResponse.Fail("Validation failed.", errors, "trace-1");

        Assert.False(response.Success);
        Assert.Null(response.Data);
        Assert.Equal("Validation failed.", response.Message);
        Assert.Equal(errors, response.Errors);
        Assert.Equal("trace-1", response.TraceId);
    }

    [Fact]
    public void Fail_without_errors_returns_an_empty_list_not_null()
    {
        var response = ApiResponse.Fail("Resource not found.");

        Assert.NotNull(response.Errors);
        Assert.Empty(response.Errors);
    }
}
