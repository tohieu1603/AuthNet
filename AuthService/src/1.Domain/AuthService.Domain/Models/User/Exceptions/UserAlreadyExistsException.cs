using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class UserAlreadyExistsException : DomainException
{
    public UserAlreadyExistsException(string reason)
        : base(ErrorCode.UserAlreadyExists, $"User already exists: {reason}") { }
}