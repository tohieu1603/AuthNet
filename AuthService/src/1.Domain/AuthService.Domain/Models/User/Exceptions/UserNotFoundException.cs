using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(string lookup)
        : base(ErrorCode.UserNotFound, $"User not found: {lookup}") { }
}