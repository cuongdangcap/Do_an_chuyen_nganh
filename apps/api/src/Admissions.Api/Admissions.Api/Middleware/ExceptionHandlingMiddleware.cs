using System.Net;
using System.Text.Json;
using Admissions.Api.Common;

namespace Admissions.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await WriteErrorAsync(context, HttpStatusCode.BadRequest, "VALIDATION_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled API exception. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteErrorAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string code,
        string message)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(code, message, context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
