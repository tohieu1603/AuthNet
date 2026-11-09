using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;

namespace AuthService.Application.Handlers;

public sealed class RegisterUserHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordEncoderPort passwordEncoder,
    ITokenProviderPort tokenProvider,
    TokenDomainService tokenDomainService,
    int refreshExpiryDays = 7
) : ICommandHandler<RegisterUserCommand, AuthResponseDto>
{
    private const string DefaultRole = "ROLE_USER";

    public async Task<AuthResponseDto> HandleAsync(RegisterUserCommand command, CancellationToken cancellationToken = default)
    {
        var username = Username.Of(command.Username);
        var email = Email.Of(command.Email);

        if (await userRepository.ExistsByUsernameAsync(username, cancellationToken))
        {
            throw new UserAlreadyExistsException($"username={command.Username}");
        }

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new UserAlreadyExistsException($"email={command.Email}");
        }

        var user = User.Register(
            username,
            email,
            Password.CreateRaw(command.RawPassword),
            PersonName.Of(command.FirstName, command.LastName),
            passwordEncoder
        );

        var defaultRole = await roleRepository.FindByNameAsync(RoleName.Of(DefaultRole), cancellationToken);
        if (defaultRole != null)
        {
            user.AssignRole(defaultRole.Id);
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
}
