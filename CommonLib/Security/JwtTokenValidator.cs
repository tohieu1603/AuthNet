namespace CommonLib.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Stateless JWT validation helper shared across services.
/// Deliberately framework-free: no dependency injection required, easy to reuse in gateways, controllers, or background services.
/// </summary>
public class JwtTokenValidator
{
    private readonly TokenValidationParameters _validationParameters;
    private static readonly JwtSecurityTokenHandler TokenHandler = new();
    private const string PlaceholderSecretPrefix = "change-me";

    public JwtTokenValidator(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException("jwt secret must be at least 32 characters (HS256 requirement)");
        }

        if (secret.StartsWith(PlaceholderSecretPrefix, StringComparison.OrdinalIgnoreCase) && IsProdProfile())
        {
            throw new InvalidOperationException(
                "Default placeholder jwt secret detected with prod profile active — " +
                "set the JWT_SECRET environment variable to a strong secret");
        }

        var keyBytes = Encoding.UTF8.GetBytes(secret);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.FromSeconds(30),
            ValidateLifetime = false // Expiry checked separately
        };
    }

    private static bool IsProdProfile()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                  ?? Environment.GetEnvironmentVariable("SPRING_PROFILES_ACTIVE")
                  ?? string.Empty;

        return env.Contains("prod", StringComparison.OrdinalIgnoreCase);
    }

    public bool ValidateSignature(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            TokenHandler.ValidateToken(token, _validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ClaimsPrincipal ParseClaims(string token)
    {
        return TokenHandler.ValidateToken(token, _validationParameters, out _);
    }

    public JwtSecurityToken ReadJwtToken(string token)
    {
        return TokenHandler.ReadJwtToken(token);
    }

    public string? ExtractUsername(string token)
    {
        var principal = ParseClaims(token);
        return principal.FindFirst("sub")?.Value 
               ?? principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    public string? ExtractUserId(string token)
    {
        var principal = ParseClaims(token);
        return principal.FindFirst("userId")?.Value 
               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? ExtractTokenId(string token)
    {
        var principal = ParseClaims(token);
        return principal.FindFirst("jti")?.Value 
               ?? principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    }

    public int ExtractTokenVersion(string token)
    {
        var principal = ParseClaims(token);
        var val = principal.FindFirst("tokenVersion")?.Value;
        return int.TryParse(val, out var v) ? v : 0;
    }

    public IReadOnlyList<string> ExtractRoles(string token)
    {
        var principal = ParseClaims(token);
        var roles = principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("roles"))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return roles;
    }

    public bool IsExpired(string token)
    {
        try
        {
            var jwt = TokenHandler.ReadJwtToken(token);
            return jwt.ValidTo < DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    public AuthenticatedUser? ExtractAuthenticatedUser(string token)
    {
        if (!ValidateSignature(token)) return null;

        var userId = ExtractUserId(token);
        var username = ExtractUsername(token);
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
        {
            return null;
        }

        var roles = ExtractRoles(token);
        return AuthenticatedUser.Of(userId, username, roles);
    }
}
