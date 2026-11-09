using AuthService.Application.Common;
using AuthService.Application.DTOs;

namespace AuthService.Application.Commands;

public sealed record RegisterUserCommand(
    string Username,
    string Email,
    string RawPassword,
    string FirstName,
    string LastName
) : ICommand<AuthResponseDto>;

public sealed record LoginCommand(
    string UsernameOrEmail,
    string RawPassword
) : ICommand<AuthResponseDto>;

public sealed record LoginWithGoogleCommand(
    string IdToken
) : ICommand<AuthResponseDto>;

public sealed record RefreshTokenCommand(
    string RefreshToken
) : ICommand<AuthResponseDto>;

public sealed record LogoutCommand(
    string? AccessToken,
    string? RefreshToken
) : ICommand<bool>;

public sealed record ChangePasswordCommand(
    string UserId,
    string OldRawPassword,
    string NewRawPassword,
    string? CurrentAccessTokenJti,
    DateTimeOffset? CurrentAccessTokenExp
) : ICommand<bool>;

public sealed record UpdateEmailCommand(
    string UserId,
    string NewEmail
) : ICommand<UserDto>;

public sealed record ChangeAccountStatusCommand(
    string UserId,
    ChangeAccountStatusCommand.AccountTransition Transition
) : ICommand<bool>
{
    public enum AccountTransition { Lock, Unlock, Enable, Disable }
}

public sealed record AssignRoleCommand(
    string UserId,
    string RoleName
) : ICommand<bool>;

public sealed record UnassignRoleCommand(
    string UserId,
    string RoleName
) : ICommand<bool>;
