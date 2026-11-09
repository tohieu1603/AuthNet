namespace ApiGateway.Middleware;

/// <summary>
/// Distributed Tracing Middleware:
/// Ensures every request flowing through the Gateway has an 'X-Correlation-Id'
/// (reuses incoming client ID or generates a new GUID) and attaches it to both Request and Response headers.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string CorrelationHeader = CommonLib.Security.AuthHeaders.CorrelationId;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(CorrelationHeader, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.Request.Headers[CorrelationHeader] = correlationId;
        }

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationHeader))
            {
                context.Response.Headers[CorrelationHeader] = correlationId;
            }
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { [CorrelationHeader] = correlationId.ToString() }))
        {
            await next(context);
        }
    }
}
