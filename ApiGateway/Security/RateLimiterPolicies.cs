using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ApiGateway.Security;

/// <summary>
/// Centralized Rate Limiter policy definitions.
/// Separates rate-limiting configuration from Program.cs for modularity.
/// </summary>
public static class RateLimiterPolicies
{
    /// <summary>
    /// Registers all gateway rate-limiting policies.
    /// </summary>
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // ── Auth Policy: Strict rate limiting for sensitive routes ────────
            // 20 requests/minute per IP (login, register, refresh)
            options.AddPolicy("auth-policy", context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // ── Default Policy: High throughput for general microservices ─────
            // 100 requests/minute per IP (catalog, cart, orders, payments)
            options.AddPolicy("default-policy", context =>
            {
                var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 20
                });
            });

            // ── User-scoped Policy: Per-user rate limiting for authenticated routes ──
            // 200 requests/minute per authenticated user
            options.AddPolicy("user-policy", context =>
            {
                var userId = context.Request.Headers[CommonLib.Security.AuthHeaders.UserId].ToString();
                var partitionKey = !string.IsNullOrEmpty(userId)
                    ? $"user:{userId}"
                    : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 10
                });
            });
        });

        return services;
    }
}
