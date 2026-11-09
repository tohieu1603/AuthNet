using System.Security.Claims;
using AuthService.Api.DTOs;
using AuthService.Api.Support;
using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// Authentication endpoints — register, login, Google SSO, refresh, logout,
/// change-password, and the "who am I?" <c>/me</c> route.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(
    ICommandHandler<RegisterUserCommand, AuthResponseDto> registerHandler,
    ICommandHandler<LoginCommand, AuthResponseDto> loginHandler,
    ICommandHandler<LoginWithGoogleCommand, AuthResponseDto> googleLoginHandler,
    ICommandHandler<RefreshTokenCommand, AuthResponseDto> refreshHandler,
    ICommandHandler<LogoutCommand, bool> logoutHandler,
    ICommandHandler<ChangePasswordCommand, bool> changePasswordHandler,
    ITokenProviderPort tokenProvider,
    AuthCookieWriter cookieWriter
) : ControllerBase
{
    /// <summary>Current authenticated principal — safe projection.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<AuthMeResponse>(200)]
    public IActionResult Me()
    {
        var userId       = User.FindFirstValue("userId") ?? string.Empty;
        var username     = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var email        = User.FindFirstValue("email") ?? string.Empty;
        var roles        = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var tokenVersion = int.TryParse(User.FindFirstValue("tokenVersion"), out var tv) ? tv : 0;

        return Ok(new AuthMeResponse(userId, username, email, roles, tokenVersion));
    }

    /// <summary>Register a new user account and set auth cookies.</summary>
    [HttpPost("register")]
    [ProducesResponseType<AuthResponseDto>(200)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var tokens = await registerHandler.HandleAsync(new RegisterUserCommand(
            request.Username, request.Email, request.Password,
            request.FirstName, request.LastName), ct);

        return WriteCookiesAndReturn(tokens);
    }

    /// <summary>Username/email + password login → HttpOnly cookies.</summary>
    [HttpPost("login")]
    [ProducesResponseType<AuthResponseDto>(200)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var tokens = await loginHandler.HandleAsync(
            new LoginCommand(request.UsernameOrEmail, request.Password), ct);

        return WriteCookiesAndReturn(tokens);
    }

    /// <summary>Login with Google (ID token from Google Identity Services).</summary>
    [HttpPost("google")]
    [ProducesResponseType<AuthResponseDto>(200)]
    public async Task<IActionResult> LoginWithGoogle([FromBody] GoogleLoginRequest request, CancellationToken ct)
    {
        var tokens = await googleLoginHandler.HandleAsync(
            new LoginWithGoogleCommand(request.IdToken), ct);

        return WriteCookiesAndReturn(tokens);
    }

    /// <summary>
    /// Rotate refresh token (cookie-first, falls back to body).
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType<AuthResponseDto>(200)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest? body, CancellationToken ct)
    {
        var refreshToken = ReadRefreshToken(body);
        var tokens = await refreshHandler.HandleAsync(new RefreshTokenCommand(refreshToken ?? string.Empty), ct);

        return WriteCookiesAndReturn(tokens);
    }

    /// <summary>Revoke session — blacklists access JTI and revokes refresh token.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var accessToken  = ReadAccessToken();
        var refreshToken = Request.Cookies.TryGetValue(AuthCookieWriter.RefreshCookie, out var r) ? r : null;

        await logoutHandler.HandleAsync(new LogoutCommand(accessToken, refreshToken), ct);

        cookieWriter.Expire(Response);
        return NoContent();
    }

    /// <summary>Change current user's password — invalidates every outstanding token.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue("userId")!;

        // Extract JTI from the current access token for immediate blacklist
        string? jti = null;
        DateTimeOffset? exp = null;
        var rawToken = ReadAccessToken();
        if (rawToken is not null)
        {
            try
            {
                var claims = tokenProvider.ParseAccessToken(rawToken);
                jti = claims.TokenId;
                exp = claims.ExpiresAt;
            }
            catch
            {
                // Malformed token — handler will skip blacklist
            }
        }

        await changePasswordHandler.HandleAsync(
            new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword, jti, exp), ct);

        cookieWriter.Expire(Response);
        return NoContent();
    }

    // ── Cookie / header helpers ─────────────────────────────────────────

    private IActionResult WriteCookiesAndReturn(AuthResponseDto tokens)
    {
        cookieWriter.WriteTokens(Response, tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresInSeconds);

        // Strip raw tokens from body — only user profile remains
        var body = tokens with { AccessToken = null, RefreshToken = null };
        return Ok(body);
    }

    /// <summary>ACCESS_TOKEN cookie (preferred) or Authorization header.</summary>
    private string? ReadAccessToken()
    {
        if (Request.Cookies.TryGetValue(AuthCookieWriter.AccessCookie, out var cookie)
            && !string.IsNullOrWhiteSpace(cookie))
        {
            return cookie;
        }

        ReadOnlySpan<char> header = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (header.StartsWith(prefix, StringComparison.Ordinal))
        {
            var token = header[prefix.Length..].Trim();
            return token.IsEmpty ? null : token.ToString();
        }

        return null;
    }

    /// <summary>Refresh token from cookie first, then fallback JSON body.</summary>
    private string? ReadRefreshToken(RefreshRequest? body)
    {
        if (Request.Cookies.TryGetValue(AuthCookieWriter.RefreshCookie, out var cookie)
            && !string.IsNullOrWhiteSpace(cookie))
        {
            return cookie;
        }

        return body?.RefreshToken;
    }
}
