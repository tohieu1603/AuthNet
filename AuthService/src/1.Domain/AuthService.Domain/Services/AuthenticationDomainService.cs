using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.User;

namespace AuthService.Domain.Services;

/// <summary>
/// Cross-aggregate authorisation rules (permission/role checks) that span
/// User, Role, and Permission aggregates.
/// </summary>
public class AuthenticationDomainService
{
    public bool HasPermission(
        User user,
        IEnumerable<Role> userRoles,
        IEnumerable<Permission> allSystemPermissions,
        string permissionName)
    {
        var grantedPermissionIds = ExtractPermissionIds(userRoles);
        return allSystemPermissions
            .Where(p => grantedPermissionIds.Contains(p.Id.Value))
            .Any(p => p.Name.Value.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasRole(User user, IEnumerable<Role> userRoles, string roleName)
    {
        return userRoles.Any(r => r.Name.Value.Equals(roleName, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlySet<string> GetPermissionNames(
        User user,
        IEnumerable<Role> userRoles,
        IEnumerable<Permission> allSystemPermissions)
    {
        var grantedPermissionIds = ExtractPermissionIds(userRoles);
        return allSystemPermissions
            .Where(p => grantedPermissionIds.Contains(p.Id.Value))
            .Select(p => p.Name.Value)
            .ToHashSet();
    }

    public bool CanAccessResource(
        User user,
        IEnumerable<Role> userRoles,
        IEnumerable<Permission> allSystemPermissions,
        string resource,
        string action)
    {
        var grantedPermissionIds = ExtractPermissionIds(userRoles);
        return allSystemPermissions
            .Where(p => grantedPermissionIds.Contains(p.Id.Value))
            .Any(p => p.Grants(resource, action));
    }

    private static HashSet<string> ExtractPermissionIds(IEnumerable<Role> roles)
    {
        return roles
            .SelectMany(r => r.Permissions)
            .Select(p => p.Value)
            .ToHashSet();
    }
}
