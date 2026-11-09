using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;

namespace AuthService.Application.Handlers;

public sealed class ChangeAccountStatusHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository
) : ICommandHandler<ChangeAccountStatusCommand, bool>
{
    public async Task<bool> HandleAsync(ChangeAccountStatusCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(UserId.Of(command.UserId), cancellationToken)
            ?? throw new UserNotFoundException(command.UserId);

        switch (command.Transition)
        {
            case ChangeAccountStatusCommand.AccountTransition.Lock:
                user.Lock();
                break;
            case ChangeAccountStatusCommand.AccountTransition.Unlock:
                user.Unlock();
                break;
            case ChangeAccountStatusCommand.AccountTransition.Enable:
                user.Enable();
                break;
            case ChangeAccountStatusCommand.AccountTransition.Disable:
                user.Disable();
                break;
        }

        if (command.Transition is ChangeAccountStatusCommand.AccountTransition.Lock or ChangeAccountStatusCommand.AccountTransition.Disable)
        {
            user.IncrementTokenVersion();
            await refreshTokenRepository.RevokeAllTokensForUserAsync(user.Id, cancellationToken);
        }

        await userRepository.SaveAsync(user, cancellationToken);
        return true;
    }
}
