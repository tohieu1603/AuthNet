using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;

namespace AuthService.Application.Handlers;

public sealed class LoginHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordEncoderPort passwordEncoder,
    ITokenProviderPort tokenProvider,
    TokenDomainService tokenDomainService,
    int refreshExpiryDays = 7
) : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await LookupAsync(command.UsernameOrEmail, cancellationToken)
            ?? throw new InvalidCredentialsException();

        bool ok = user.Authenticate(Password.CreateRaw(command.RawPassword), passwordEncoder);
        if (!ok)
        {
            throw new InvalidCredentialsException();
        }

        var saved = await userRepository.SaveAsync(user, cancellationToken);
        var userRoles = await roleRepository.FindByIdInAsync(saved.Roles, cancellationToken);
        var roleNames = userRoles.Select(r => r.Name.Value).ToHashSet();

        var issued = tokenProvider.IssueAccessToken(saved, roleNames);
        var refresh = tokenDomainService.IssueForUser(saved, refreshExpiryDays);
        await refreshTokenRepository.SaveAsync(refresh, cancellationToken);

        return AuthResponseDto.Bearer(
            issued.Token,
            refresh.Value.Value,
            issued.ExpiresInSeconds,
            UserDtoMapper.ToDto(saved, userRoles)
        );
    }

    private async Task<User?> LookupAsync(string? usernameOrEmail, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail)) return null;

        try
        {
            return usernameOrEmail.Contains('@')
                ? await userRepository.FindByEmailAsync(Email.Of(usernameOrEmail), cancellationToken)
                : await userRepository.FindByUsernameAsync(Username.Of(usernameOrEmail), cancellationToken);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
