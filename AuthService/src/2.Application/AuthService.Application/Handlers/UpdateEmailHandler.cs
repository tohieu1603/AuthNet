using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;

namespace AuthService.Application.Handlers;

public sealed class UpdateEmailHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : ICommandHandler<UpdateEmailCommand, UserDto>
{
    public async Task<UserDto> HandleAsync(UpdateEmailCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(UserId.Of(command.UserId), cancellationToken)
            ?? throw new UserNotFoundException(command.UserId);

        var newEmail = Email.Of(command.NewEmail);
        if (!user.Email.Equals(newEmail) && await userRepository.ExistsByEmailAsync(newEmail, cancellationToken))
        {
            throw new UserAlreadyExistsException($"email={command.NewEmail}");
        }

        user.UpdateEmail(newEmail);
        var saved = await userRepository.SaveAsync(user, cancellationToken);
        var userRoles = await roleRepository.FindByIdInAsync(saved.Roles, cancellationToken);

        return UserDtoMapper.ToDto(saved, userRoles);
    }
}
