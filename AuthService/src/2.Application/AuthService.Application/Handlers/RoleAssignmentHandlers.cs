using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;

namespace AuthService.Application.Handlers;

public sealed class AssignRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : ICommandHandler<AssignRoleCommand, bool>
{
    public async Task<bool> HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(UserId.Of(command.UserId), cancellationToken)
            ?? throw new UserNotFoundException(command.UserId);

        var role = await roleRepository.FindByNameAsync(RoleName.Of(command.RoleName), cancellationToken)
            ?? throw new ArgumentException($"Role not found: {command.RoleName}");

        user.AssignRole(role.Id);
        await userRepository.SaveAsync(user, cancellationToken);
        return true;
    }
}

public sealed class UnassignRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : ICommandHandler<UnassignRoleCommand, bool>
{
    public async Task<bool> HandleAsync(UnassignRoleCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdAsync(UserId.Of(command.UserId), cancellationToken)
            ?? throw new UserNotFoundException(command.UserId);

        var role = await roleRepository.FindByNameAsync(RoleName.Of(command.RoleName), cancellationToken)
            ?? throw new ArgumentException($"Role not found: {command.RoleName}");

        user.UnassignRole(role.Id);
        await userRepository.SaveAsync(user, cancellationToken);
        return true;
    }
}
