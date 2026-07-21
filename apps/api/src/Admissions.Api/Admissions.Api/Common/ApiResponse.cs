namespace Admissions.Api.Common;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string Message,
    string? TraceId = null,
    ApiError? Error = null)
{
    public static ApiResponse<T> Ok(T data, string message = "OK", string? traceId = null)
    {
        return new ApiResponse<T>(true, data, message, traceId);
    }

    public static ApiResponse<T> Fail(string code, string message, string? traceId = null, object? details = null)
    {
        return new ApiResponse<T>(false, default, message, traceId, new ApiError(code, message, details));
    }
}

public sealed record ApiError(string Code, string Message, object? Details = null);
