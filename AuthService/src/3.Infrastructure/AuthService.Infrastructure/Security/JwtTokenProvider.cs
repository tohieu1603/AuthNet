using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AuthService.Domain.Models.User;
using AuthService.Domain.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public class JwtTokenProvider : ITokenProviderPort
{
    private readonly JwtOptions _options;
    private readonly SymmetricSecurityKey _key;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public JwtTokenProvider(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        if (string.IsNullOrWhiteSpace(_options.Secret) || _options.Secret.Length < 32)
        {
            throw new InvalidOperationException("Jwt.Secret must be at least 32 characters long.");
        }
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
    }

    public ITokenProviderPort.IssuedAccessToken IssueAccessToken(User user, IReadOnlySet<string> roles)
    {
        var now = DateTimeOffset.UtcNow;
        var exp = now.AddSeconds(_options.AccessExpirationSeconds);
        string tokenId = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(JwtRegisteredClaimNames.Sub, user.Username.Value),
            new(JwtRegisteredClaimNames.Iss, _options.Issuer),
            new("userId", user.Id.Value),
            new("email", user.Email.Value),
            new("tokenVersion", user.TokenVersion.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("roles", role));
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = exp.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Issuer = _options.Issuer,
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256)
        };

        var securityToken = _tokenHandler.CreateToken(tokenDescriptor);
        string token = _tokenHandler.WriteToken(securityToken);

        return new ITokenProviderPort.IssuedAccessToken(token, tokenId, exp, _options.AccessExpirationSeconds);
    }

    public ITokenProviderPort.AccessClaims ParseAccessToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
        var jwt = (JwtSecurityToken)validatedToken;

        string tokenId = jwt.Id ?? principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? string.Empty;
        string userId = principal.FindFirst("userId")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        string username = jwt.Subject ?? principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        int tokenVersion = 0;
        var tvClaim = principal.FindFirst("tokenVersion");
        if (tvClaim != null && int.TryParse(tvClaim.Value, out int tv))
        {
            tokenVersion = tv;
        }

        var roles = principal.FindAll("roles")
            .Concat(principal.FindAll(ClaimTypes.Role))
            .Select(c => c.Value)
            .ToHashSet();

        return new ITokenProviderPort.AccessClaims(
            tokenId,
            userId,
            username,
            tokenVersion,
            roles,
            new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero)
        );
    }

    public bool IsSignatureValid(string token)
    {
        try
        {
            ParseAccessToken(token);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
