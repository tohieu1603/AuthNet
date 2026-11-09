using AuthService.Application.DTOs;
using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.User;

namespace AuthService.Application.Mappers;

/// <summary>
/// Mapper from domain aggregates to read-model DTOs.
/// </summary>
public static class UserDtoMapper
{
    public static UserDto ToDto(
        User user,
        IEnumerable<Role> roles,
        IEnumerable<Permission>? effectivePermissions = null)
    {
        var roleList = roles as IReadOnlyList<Role> ?? roles.ToList();
        var roleNames = roleList.Select(r => r.Name.Value).ToHashSet();

        HashSet<string> permissionNames;
        if (effectivePermissions != null)
        {
            permissionNames = effectivePermissions.Select(p => p.Name.Value).ToHashSet();
        }
        else
        {
            permissionNames = [];
        }

        return new UserDto(
            Id: user.Id.Value,
            Username: user.Username.Value,
            Email: user.Email.Value,
            FirstName: user.PersonName.FirstName,
            LastName: user.PersonName.LastName,
            Enabled: user.AccountStatus.IsEnabled,
            AccountNonExpired: user.AccountStatus.IsAccountNonExpired,
            AccountNonLocked: user.AccountStatus.IsAccountNonLocked,
            CredentialsNonExpired: user.AccountStatus.IsCredentialsNonExpired,
            Roles: roleNames,
            Permissions: permissionNames,
            CreatedAt: user.CreatedAt,
            UpdatedAt: user.UpdatedAt,
            LastLogin: user.AccountStatus.LastLogin
        );
    }

    public static RoleDto ToDto(Role role, IEnumerable<string> grantedPermissions)
    {
        return new RoleDto(
            Id: role.Id.Value,
            Name: role.Name.Value,
            Description: role.Description,
            Permissions: grantedPermissions.ToHashSet(),
            CreatedAt: role.CreatedAt,
            UpdatedAt: role.UpdatedAt
        );
    }

    public static PermissionDto ToDto(Permission permission)
    {
        return new PermissionDto(
            Id: permission.Id.Value,
            Name: permission.Name.Value,
            Resource: permission.Name.Resource,
            Action: permission.Name.Action,
            Description: permission.Description,
            CreatedAt: permission.CreatedAt,
            UpdatedAt: permission.UpdatedAt
        );
    }
}
