using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class OAuthEmailNotVerifiedException : DomainException
{
    public OAuthEmailNotVerifiedException()
        : base(ErrorCode.OAuthEmailUnverified, "OAuth provider email is not verified") { }
}