using Vas.Shared.Errors;

namespace Vas.Shared.Results;

public sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    IReadOnlyList<ErrorDetail> Errors,
    string? TraceId);

public static class ApiResponse
{
    public const string DefaultSuccessMessage = "Operation completed successfully.";

    public static ApiResponse<T> Ok<T>(T data, string message = DefaultSuccessMessage) =>
        new(true, message, data, [], null);

    public static ApiResponse<object> Fail(
        string message,
        IReadOnlyList<ErrorDetail>? errors = null,
        string? traceId = null) =>
        new(false, message, null, errors ?? [], traceId);
}
