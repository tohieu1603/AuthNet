using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.RefreshToken.Exceptions;

public sealed class TokenRevokedException : DomainException
{
    public string? TokenId { get; }
    public string? Reason { get; }

    public TokenRevokedException(string? tokenId, string? reason)
        : base(ErrorCode.TokenRevoked, "Refresh token has been revoked.")
    {
        TokenId = tokenId;
        Reason = reason;
    }
}