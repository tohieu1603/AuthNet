using System.Security.Claims;
using AuthService.Api.Support;
using AuthService.Application.Ports;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AuthService.Api.Middleware;

/// <summary>
/// Stateless JWT authentication middleware — .NET equivalent of Spring's
/// <c>JwtAuthenticationFilter</c>. On every request:
/// <list type="number">
///   <item>Extracts bearer from <c>ACCESS_TOKEN</c> cookie or <c>Authorization</c> header.</item>
///   <item>Parses + signature-verifies via <see cref="ITokenProviderPort"/>.</item>
///   <item>Rejects if <c>jti</c> is blacklisted.</item>
///   <item>Loads the live user to check <c>tokenVersion</c> and account status.</item>
///   <item>Populates <see cref="HttpContext.User"/> with a <see cref="ClaimsPrincipal"/>.</item>
/// </list>
/// Missing or invalid tokens silently leave the context unauthenticated.
/// </summary>
public sealed class JwtAuthenticationMiddleware(RequestDelegate next, ILogger<JwtAuthenticationMiddleware> logger)
{
    private const string BearerPrefix = "Bearer ";

    public async Task InvokeAsync(
        HttpContext context,
        ITokenProviderPort tokenProvider,
        ITokenBlacklistPort tokenBlacklist,
        IUserRepository userRepository)
    {
        ReadOnlySpan<char> raw = ExtractBearer(context.Request);

        if (!raw.IsEmpty && context.User.Identity is { IsAuthenticated: false })
        {
            string token = raw.ToString();
            try
            {
                var claims = tokenProvider.ParseAccessToken(token);

                if (await tokenBlacklist.IsRevokedAsync(claims.TokenId, context.RequestAborted))
                {
                    logger.LogDebug("Rejected revoked token jti={Jti}", claims.TokenId);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                // Verify tokenVersion + account status against live DB state
                var user = await userRepository.FindByIdAsync(
                    Domain.Models.User.VO.UserId.Of(claims.UserId), context.RequestAborted);

                if (user is null)
                {
                    logger.LogDebug("JWT principal not found: userId={UserId}", claims.UserId);
                    await next(context);
                    return;
                }

                if (user.TokenVersion != claims.TokenVersion)
                {
                    logger.LogDebug("tokenVersion mismatch userId={UserId} (jwt={Jwt}, current={Current})",
                        claims.UserId, claims.TokenVersion, user.TokenVersion);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                if (!user.IsActive)
                {
                    logger.LogDebug("Rejected token for inactive account userId={UserId}", claims.UserId);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                // Build ClaimsPrincipal — roles as ClaimTypes.Role for [Authorize(Roles="...")]
                List<Claim> identityClaims =
                [
                    new(ClaimTypes.NameIdentifier, claims.UserId),
                    new(ClaimTypes.Name, claims.Username),
                    new("userId", claims.UserId),
                    new("email", user.Email.Value),
                    new("tokenVersion", claims.TokenVersion.ToString()),
                    new("jti", claims.TokenId),
                    .. claims.Roles.Select(r => new Claim(ClaimTypes.Role, r))
                ];

                var identity  = new ClaimsIdentity(identityClaims, "Bearer");
                context.User  = new ClaimsPrincipal(identity);
            }
            catch (Exception ex)
            {
                // Expired / malformed tokens are normal public-endpoint traffic
                logger.LogDebug("JWT parse failed: {Message}", ex.Message);
            }
        }

        await next(context);
    }

    /// <summary>
    /// ACCESS_TOKEN cookie first, then <c>Authorization: Bearer …</c> header.
    /// Returns <see cref="ReadOnlySpan{T}"/> to avoid allocation for the common no-token path.
    /// </summary>
    private static ReadOnlySpan<char> ExtractBearer(HttpRequest request)
    {
        // Cookie-first (browser clients)
        if (request.Cookies.TryGetValue(AuthCookieWriter.AccessCookie, out var cookieValue)
            && !string.IsNullOrWhiteSpace(cookieValue))
        {
            return cookieValue.AsSpan();
        }

        // Header fallback (API / mobile / CLI clients)
        string? header = request.Headers.Authorization.ToString();
        if (header is { Length: > 0 } && header.AsSpan().StartsWith(BearerPrefix.AsSpan(), StringComparison.Ordinal))
        {
            ReadOnlySpan<char> token = header.AsSpan()[BearerPrefix.Length..].Trim();
            return token.IsEmpty ? [] : token;
        }

        return [];
    }
}
