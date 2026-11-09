using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.RefreshToken.Exceptions;

public sealed class TokenOwnershipException : DomainException
{
    public string UserId { get; }

    public TokenOwnershipException(string userId)
        : base(ErrorCode.TokenOwnershipFail, "Refresh token does not belong to the user.")
    {
        UserId = userId;
    }
}