using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.Ports;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;

namespace AuthService.Application.Handlers;

public sealed class LogoutHandler(
    IRefreshTokenRepository refreshTokenRepository,
    ITokenProviderPort tokenProvider,
    ITokenBlacklistPort tokenBlacklist
) : ICommandHandler<LogoutCommand, bool>
{
    public async Task<bool> HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            var token = await refreshTokenRepository.FindByTokenValueAsync(TokenValue.Of(command.RefreshToken), cancellationToken);
            if (token != null)
            {
                token.Revoke(RevokedReason.UserInitiated);
                await refreshTokenRepository.SaveAsync(token, cancellationToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(command.AccessToken))
        {
            try
            {
                var claims = tokenProvider.ParseAccessToken(command.AccessToken);
                await tokenBlacklist.RevokeAsync(claims.TokenId, claims.UserId, claims.ExpiresAt, "LOGOUT", cancellationToken);
            }
            catch
            {
                // Access token may already be expired or invalid; refresh token is already revoked.
            }
        }

        return true;
    }
}
