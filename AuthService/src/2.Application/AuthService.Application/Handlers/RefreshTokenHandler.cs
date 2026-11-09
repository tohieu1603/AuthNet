using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Domain.Models.RefreshToken.Exceptions;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;

namespace AuthService.Application.Handlers;

public sealed class RefreshTokenHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    TokenDomainService tokenDomainService,
    ITokenProviderPort tokenProvider,
    int refreshExpiryDays = 7
) : ICommandHandler<RefreshTokenCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var presented = await refreshTokenRepository.FindByTokenValueForUpdateAsync(
            TokenValue.Of(command.RefreshToken), cancellationToken)
            ?? throw new TokenRevokedException(null, null);

        var rotated = await tokenDomainService.RotateAsync(presented, refreshExpiryDays, refreshTokenRepository, cancellationToken);
        await refreshTokenRepository.SaveAsync(rotated, cancellationToken);

        var user = await userRepository.FindByIdAsync(presented.UserId, cancellationToken)
            ?? throw new UserNotFoundException(presented.UserId.Value);

        user.EnsureAuthenticatable();

        var userRoles = await roleRepository.FindByIdInAsync(user.Roles, cancellationToken);
        var roleNames = userRoles.Select(r => r.Name.Value).ToHashSet();

        var issued = tokenProvider.IssueAccessToken(user, roleNames);

        return AuthResponseDto.Bearer(
            issued.Token,
            rotated.Value.Value,
            issued.ExpiresInSeconds,
            UserDtoMapper.ToDto(user, userRoles)
        );
    }
}
