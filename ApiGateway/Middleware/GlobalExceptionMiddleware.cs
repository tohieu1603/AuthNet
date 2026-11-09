using System.Net;
using System.Text.Json;

namespace ApiGateway.Middleware;

/// <summary>
/// Gateway Global Exception Middleware:
/// Catches downstream connection drops, timeouts, and unhandled proxy faults,
/// translating them into canonical machine-readable error responses.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request cancelled by client on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (TimeoutException ex)
        {
            logger.LogWarning(ex, "Gateway downstream timeout on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.GatewayTimeout, CommonLib.Error.ErrorCode.GatewayTimeout, "Upstream service timeout");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Gateway failed to connect to downstream service on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.BadGateway, CommonLib.Error.ErrorCode.GatewayBadGateway, "Upstream service unreachable");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Gateway unexpected failure on {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, HttpStatusCode.InternalServerError, CommonLib.Error.ErrorCode.GatewayInternalError, "Gateway internal error");
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, HttpStatusCode status, string code, string message)
    {
        if (context.Response.HasStarted) return;

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";

        var correlationId = context.Request.Headers[CommonLib.Security.AuthHeaders.CorrelationId].ToString();
        var error = CommonLib.Error.ErrorResponse.Of(code, message, context.Request.Path.Value)
            .WithTraceId(correlationId);

        await JsonSerializer.SerializeAsync(context.Response.Body, error);
    }
}
