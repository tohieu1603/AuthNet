using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.RefreshToken.Exceptions;

public sealed class TokenExpiredException : DomainException
{
    public string TokenId { get; }

    public TokenExpiredException(string tokenId)
        : base(ErrorCode.TokenExpired, "Refresh token has expired.")
    {
        TokenId = tokenId;
    }
}