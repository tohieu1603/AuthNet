using Yarp.ReverseProxy.Transforms;

namespace ApiGateway.Extensions;

/// <summary>
/// Custom YARP transform extensions.
/// Adds custom header transformations to YARP request pipeline.
/// </summary>
public static class YarpTransformExtensions
{
    /// <summary>
    /// Adds custom header transforms for YARP reverse proxy routes.
    /// - X-Forwarded-Host: Original Host header from client
    /// - X-Gateway-Timestamp: Time the request hit the gateway (ISO 8601 UTC)
    /// - X-Correlation-Id: Propagated from CorrelationIdMiddleware
    /// </summary>
    public static IReverseProxyBuilder AddGatewayTransforms(this IReverseProxyBuilder proxyBuilder)
    {
        proxyBuilder.AddTransforms(context =>
        {
            // Propagate the original Host to downstream services
            context.AddRequestTransform(transformContext =>
            {
                var request = transformContext.HttpContext.Request;

                // Forward original host
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                    "X-Forwarded-Host", request.Host.Value);

                // Timestamp of gateway processing
                transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                    "X-Gateway-Timestamp", DateTimeOffset.UtcNow.ToString("O"));

                // Propagate correlation ID from middleware
                if (request.Headers.TryGetValue(CommonLib.Security.AuthHeaders.CorrelationId, out var correlationId))
                {
                    transformContext.ProxyRequest.Headers.TryAddWithoutValidation(
                        CommonLib.Security.AuthHeaders.CorrelationId, correlationId.ToString());
                }

                return ValueTask.CompletedTask;
            });
        });

        return proxyBuilder;
    }
}
