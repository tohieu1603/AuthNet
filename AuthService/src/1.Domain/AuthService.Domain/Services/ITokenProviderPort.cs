using AuthService.Domain.Models.User;

namespace AuthService.Domain.Services;

/// <summary>
/// Outbound port for access-token issuance and inspection.
/// </summary>
public interface ITokenProviderPort
{
    public sealed record IssuedAccessToken(
        string Token,
        string TokenId,
        DateTimeOffset ExpiresAt,
        long ExpiresInSeconds
    );

    public sealed record AccessClaims(
        string TokenId,
        string UserId,
        string Username,
        int TokenVersion,
        IReadOnlySet<string> Roles,
        DateTimeOffset ExpiresAt
    );

    IssuedAccessToken IssueAccessToken(User user, IReadOnlySet<string> roles);
    AccessClaims ParseAccessToken(string token);
    bool IsSignatureValid(string token);
}
