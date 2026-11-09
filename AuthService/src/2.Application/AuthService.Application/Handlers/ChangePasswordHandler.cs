using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.Ports;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;

namespace AuthService.Application.Handlers;

public sealed class ChangePasswordHandler(
    IUserRepository userRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordEncoderPort passwordEncoder,
    ITokenBlacklistPort tokenBlacklist
) : ICommandHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(UserId.Of(command.UserId), cancellationToken)
            ?? throw new UserNotFoundException(command.UserId);

        user.ChangePassword(
            Password.CreateRaw(command.OldRawPassword),
            Password.CreateRaw(command.NewRawPassword),
            passwordEncoder
        );

        await userRepository.SaveAsync(user, cancellationToken);
        await refreshTokenRepository.RevokeAllTokensForUserAsync(user.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(command.CurrentAccessTokenJti) && command.CurrentAccessTokenExp.HasValue)
        {
            await tokenBlacklist.RevokeAsync(
                command.CurrentAccessTokenJti,
                command.UserId,
                command.CurrentAccessTokenExp.Value,
                "PASSWORD_CHANGED",
                cancellationToken
            );
        }

        return true;
    }
}
