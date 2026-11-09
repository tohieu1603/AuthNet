using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Mappers;
using AuthService.Application.Queries;
using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;

namespace AuthService.Application.Handlers;

public sealed class GetUserByIdHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository
) : IQueryHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdWithRolesAsync(UserId.Of(query.UserId), cancellationToken)
            ?? throw new UserNotFoundException(query.UserId);

        var roles = await roleRepository.FindByIdInAsync(user.Roles, cancellationToken);
        var permIds = roles.SelectMany(r => r.Permissions).ToHashSet();
        var permissions = permIds.Count > 0
            ? await permissionRepository.FindByIdInAsync(permIds, cancellationToken)
            : [];

        return UserDtoMapper.ToDto(user, roles, permissions);
    }
}

public sealed class GetUserByUsernameHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : IQueryHandler<GetUserByUsernameQuery, UserDto>
{
    public async Task<UserDto> HandleAsync(GetUserByUsernameQuery query, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByUsernameWithRolesAsync(Username.Of(query.Username), cancellationToken)
            ?? throw new UserNotFoundException(query.Username);

        var roles = await roleRepository.FindByIdInAsync(user.Roles, cancellationToken);
        return UserDtoMapper.ToDto(user, roles);
    }
}

public sealed class ListUsersHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : IQueryHandler<ListUsersQuery, PageDto<UserDto>>
{
    private const int MaxLimit = 100;

    public async Task<PageDto<UserDto>> HandleAsync(ListUsersQuery query, CancellationToken cancellationToken = default)
    {
        int pageSize = Math.Clamp(query.Limit, 1, MaxLimit);
        var cursor = CursorCodec.Decode(query.Cursor);

        var rows = await userRepository.FindAfterCursorAsync(
            cursor?.CreatedAt, cursor?.Id, pageSize + 1, cancellationToken);

        bool hasNext = rows.Count > pageSize;
        var pageRows = hasNext ? rows.Take(pageSize).ToList() : rows;

        var items = new List<UserDto>(pageRows.Count);
        foreach (var u in pageRows)
        {
            var roles = await roleRepository.FindByIdInAsync(u.Roles, cancellationToken);
            items.Add(UserDtoMapper.ToDto(u, roles));
        }

        string? nextCursor = null;
        if (hasNext && pageRows.Count > 0)
        {
            var last = pageRows[^1];
            nextCursor = CursorCodec.Encode(last.CreatedAt, last.Id.Value);
        }

        return PageDto<UserDto>.Of(items, nextCursor, pageSize, -1);
    }
}

public sealed class CheckRoleHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : IQueryHandler<CheckRoleQuery, bool>
{
    public async Task<bool> HandleAsync(CheckRoleQuery query, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdWithRolesAsync(UserId.Of(query.UserId), cancellationToken);
        if (user == null || user.Roles.Count == 0) return false;

        var roles = await roleRepository.FindByIdInAsync(user.Roles, cancellationToken);
        return roles.Any(r => r.Name.Value.Equals(query.RoleName, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class CheckPermissionHandler(
    IUserRepository userRepository,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository
) : IQueryHandler<CheckPermissionQuery, bool>
{
    public async Task<bool> HandleAsync(CheckPermissionQuery query, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByIdWithRolesAsync(UserId.Of(query.UserId), cancellationToken);
        if (user == null || user.Roles.Count == 0) return false;

        var roles = await roleRepository.FindByIdInAsync(user.Roles, cancellationToken);
        var permIds = roles.SelectMany(r => r.Permissions).ToHashSet();
        if (permIds.Count == 0) return false;

        var perms = await permissionRepository.FindByIdInAsync(permIds, cancellationToken);
        return perms.Any(p => p.Name.Value.Equals(query.PermissionName, StringComparison.OrdinalIgnoreCase));
    }
}
