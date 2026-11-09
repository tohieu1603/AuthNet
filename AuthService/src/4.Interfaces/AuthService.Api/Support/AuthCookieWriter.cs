using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AuthService.Infrastructure.Security;

namespace AuthService.Api.Support;

/// <summary>
/// Writes access + refresh tokens onto HttpOnly cookies and returns a sanitised body.
/// <list type="bullet">
///   <item><c>ACCESS_TOKEN</c> — HttpOnly, Secure, SameSite=Lax, path=/, max-age = access TTL.</item>
///   <item><c>REFRESH_TOKEN</c> — same flags, path=/api/v1/auth (scoped to auth endpoints only).</item>
/// </list>
/// Response body strips raw tokens so an accidental client-side log never captures them.
/// </summary>
public sealed class AuthCookieWriter(IOptions<JwtOptions> jwtOptions, IOptions<CookieSettings> cookieSettings)
{
    public const string AccessCookie  = "ACCESS_TOKEN";
    public const string RefreshCookie = "REFRESH_TOKEN";

    private const string RefreshPath = "/api/v1/auth";

    private readonly JwtOptions _jwt       = jwtOptions.Value;
    private readonly CookieSettings _cookie = cookieSettings.Value;

    /// <summary>
    /// Appends both Set-Cookie headers to the response.
    /// </summary>
    public void WriteTokens(HttpResponse response, string? accessToken, string? refreshToken, long accessTtlSeconds)
    {
        if (!string.IsNullOrEmpty(accessToken))
        {
            response.Cookies.Append(AccessCookie, accessToken, BuildOptions("/", TimeSpan.FromSeconds(accessTtlSeconds)));
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            response.Cookies.Append(RefreshCookie, refreshToken, BuildOptions(RefreshPath, TimeSpan.FromDays(_jwt.RefreshExpirationDays)));
        }
    }

    /// <summary>
    /// Emits expired cookies — used on logout to clear the browser jar.
    /// </summary>
    public void Expire(HttpResponse response)
    {
        response.Cookies.Delete(AccessCookie,  BuildOptions("/",          TimeSpan.Zero));
        response.Cookies.Delete(RefreshCookie, BuildOptions(RefreshPath, TimeSpan.Zero));
    }

    private CookieOptions BuildOptions(string path, TimeSpan maxAge) => new()
    {
        HttpOnly = true,
        Secure   = _cookie.Secure,
        SameSite = Enum.TryParse<SameSiteMode>(_cookie.SameSite, ignoreCase: true, out var mode) ? mode : SameSiteMode.Lax,
        Path     = path,
        MaxAge   = maxAge
    };
}

/// <summary>Cookie behaviour settings bound from <c>Auth:Cookie</c> configuration section.</summary>
public sealed class CookieSettings
{
    public const string SectionName = "Auth:Cookie";

    public bool Secure     { get; set; } = true;
    public string SameSite { get; set; } = "Lax";
}
