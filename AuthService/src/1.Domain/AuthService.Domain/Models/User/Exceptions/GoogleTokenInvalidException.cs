using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class GoogleTokenInvalidException : DomainException
{
    public GoogleTokenInvalidException(string details)
        : base(ErrorCode.OAuthTokenInvalid, $"Invalid Google token: {details}") { }
}
