using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiGateway.Security;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Middleware;

/// <summary>
/// Production-ready Gateway JWT filter & token revocation checker:
/// 1. Strips spoofed upstream identity headers.
/// 2. Extracts JWT from ACCESS_TOKEN cookie first, then Authorization Bearer header.
/// 3. Validates signature, expiry, and blacklisted JTI in Redis.
/// 4. Enforces authentication on protected routes (returning 401).
/// 5. Propagates verified identity headers (X-User-Id, X-User-Name, X-Token-Id, X-Token-Version) to downstream services.
/// </summary>
public sealed class GatewayAuthenticationMiddleware(
    RequestDelegate next,
    IConfiguration configuration,
    ITokenRevocationValidator revocationValidator,
    ILogger<GatewayAuthenticationMiddleware> logger)
{
    private static readonly JwtSecurityTokenHandler TokenHandler = new();
    private readonly byte[] _keyBytes = Encoding.UTF8.GetBytes(
        configuration["Jwt:Secret"] ?? "change-me-change-me-change-me-change-me-please-32chars");

    // Routes that do NOT require authentication at the gateway edge
    private static readonly string[] PublicPrefixes =
    [
        "/health",
        "/swagger",
        "/api/v1/auth/login",
        "/api/v1/auth/register",
        "/api/v1/auth/refresh",
        "/api/v1/auth/google",
        "/gateway/probe"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 1. Strip any client-spoofed headers
        context.Request.Headers.Remove(CommonLib.Security.AuthHeaders.UserId);
        context.Request.Headers.Remove(CommonLib.Security.AuthHeaders.Username);
        context.Request.Headers.Remove(CommonLib.Security.AuthHeaders.TokenId);
        context.Request.Headers.Remove(CommonLib.Security.AuthHeaders.TokenVersion);

        // Check if route is public
        bool isPublic = PublicPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

        // 2. Read token
        string? token = ReadAccessToken(context.Request);

        if (string.IsNullOrWhiteSpace(token))
        {
            if (isPublic)
            {
                await next(context);
                return;
            }

            // Protected route missing token -> 401 Unauthorized
            await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized, CommonLib.Error.ErrorCode.Unauthorized, "Missing access token");
            return;
        }

        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(_keyBytes),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.FromSeconds(30),
                ValidateLifetime = true
            };

            var principal = TokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var jti = principal.FindFirst("jti")?.Value;
            if (!string.IsNullOrEmpty(jti) && await revocationValidator.IsRevokedAsync(jti))
            {
                logger.LogWarning("Gateway rejected blacklisted token JTI: {Jti}", jti);
                await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized, CommonLib.Error.ErrorCode.TokenRevoked, "Token has been revoked");
                return;
            }

            context.User = principal;

            // 3. Forward verified identity headers to internal microservices
            var userId = principal.FindFirst("userId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.Name)?.Value;
            var tokenVersion = principal.FindFirst("tokenVersion")?.Value;

            if (!string.IsNullOrEmpty(userId)) context.Request.Headers[CommonLib.Security.AuthHeaders.UserId] = userId;
            if (!string.IsNullOrEmpty(username)) context.Request.Headers[CommonLib.Security.AuthHeaders.Username] = username;
            if (!string.IsNullOrEmpty(jti)) context.Request.Headers[CommonLib.Security.AuthHeaders.TokenId] = jti;
            if (!string.IsNullOrEmpty(tokenVersion)) context.Request.Headers[CommonLib.Security.AuthHeaders.TokenVersion] = tokenVersion;
        }
        catch (SecurityTokenExpiredException)
        {
            if (!isPublic)
            {
                await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized, CommonLib.Error.ErrorCode.TokenExpired, "Access token expired");
                return;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug("Gateway token validation failed: {Message}", ex.Message);
            if (!isPublic)
            {
                await WriteJsonErrorAsync(context, StatusCodes.Status401Unauthorized, CommonLib.Error.ErrorCode.Unauthorized, "Invalid access token");
                return;
            }
        }

        await next(context);
    }

    private static string? ReadAccessToken(HttpRequest request)
    {
        if (request.Cookies.TryGetValue("ACCESS_TOKEN", out var cookie) && !string.IsNullOrWhiteSpace(cookie))
        {
            return cookie;
        }

        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var headerStr = authHeader.ToString();
            const string prefix = "Bearer ";
            if (headerStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var t = headerStr[prefix.Length..].Trim();
                if (!string.IsNullOrEmpty(t)) return t;
            }
        }

        return null;
    }

    private static async Task WriteJsonErrorAsync(HttpContext context, int statusCode, string code, string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var err = CommonLib.Error.ErrorResponse.Of(code, message, context.Request.Path.Value);
        await System.Text.Json.JsonSerializer.SerializeAsync(context.Response.Body, err);
    }
}
