using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException()
        : base(ErrorCode.InvalidCredentials, "Invalid username or password") { }
}