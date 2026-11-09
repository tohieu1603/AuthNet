using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class OAuthAccountAlreadyLinkedException : DomainException
{
    public OAuthAccountAlreadyLinkedException(string provider)
        : base(ErrorCode.OAuthAccountLinked, $"Account already linked with another {provider} identity") { }
}