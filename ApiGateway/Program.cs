using ApiGateway.Clients;
using ApiGateway.Extensions;
using ApiGateway.Middleware;
using ApiGateway.Security;

var builder = WebApplication.CreateBuilder(args);

// ── Clean DI Registration (Extensions/) ──────────────────────────────────────
builder.Services
    .AddGatewayRedis(builder.Configuration)
    .AddGatewayRefitClients(builder.Configuration)
    .AddGatewayRateLimiting()
    .AddGatewayCors()
    .AddGatewayYarp(builder.Configuration);

var app = builder.Build();

// ── Middleware Pipeline (Order matters!) ──────────────────────────────────────
app.UseCors();
app.UseMiddleware<CorrelationIdMiddleware>();       // 1. Distributed Tracing
app.UseMiddleware<GlobalExceptionMiddleware>();     // 2. Error Translation (502/503/504)
app.UseRateLimiter();                               // 3. Edge Rate Limiter
app.UseMiddleware<GatewayAuthenticationMiddleware>();// 4. JWT + Token Revocation

// ── Health Probes ────────────────────────────────────────────────────────────
app.MapGet("/health", (HttpContext ctx) => Results.Ok(new
{
    status = "UP",
    service = "ApiGateway",
    correlationId = ctx.Request.Headers[CorrelationIdMiddleware.CorrelationHeader].ToString(),
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/gateway/probe/auth-health", async (IAuthServiceClient authClient) =>
{
    try
    {
        var res = await authClient.CheckHealthAsync();
        return Results.Ok(new { status = "AuthService reachable via Refit", code = res.StatusCode });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to reach AuthService: {ex.Message}");
    }
});

// ── YARP Reverse Proxy ───────────────────────────────────────────────────────
app.MapReverseProxy();

app.Run();
