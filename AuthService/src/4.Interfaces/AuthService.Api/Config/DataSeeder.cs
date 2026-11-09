using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Permission.VO;
using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;
using Microsoft.Extensions.Logging;

namespace AuthService.Api.Config;

/// <summary>
/// Bootstrap data — default permissions, roles, and default admin user required for the service to function.
/// Idempotent: looks up each item by name and inserts only what's missing.
/// Safe to run on every startup, including production.
/// </summary>
public sealed class DataSeeder(
    IPermissionRepository permissionRepository,
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IPasswordEncoderPort passwordEncoder,
    ILogger<DataSeeder> logger)
{
    /// <summary>Canonical resource + action combinations defining the base permission set.</summary>
    private static readonly (string Resource, string Action, string Description)[] DefaultPermissions =
    [
        ("USER",       "READ",   "Read user data"),
        ("USER",       "WRITE",  "Create / update users"),
        ("USER",       "DELETE", "Delete users"),
        ("ROLE",       "MANAGE", "Create / update / delete roles"),
        ("PERMISSION", "MANAGE", "Create / update / delete permissions"),
    ];

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // 1. Permissions — INSERT only when missing
        foreach (var (resource, action, description) in DefaultPermissions)
        {
            var name = PermissionName.Of(resource, action);
            var existing = await permissionRepository.FindByNameAsync(name, ct);
            if (existing is null)
            {
                var created = Permission.Create(resource, action, description);
                await permissionRepository.SaveAsync(created, ct);
                logger.LogInformation("[seed] Inserted permission {Name}", created.Name.Value);
            }
        }

        // 2. ROLE_USER — baseline read-only authority for every registered user
        await EnsureRoleAsync("ROLE_USER", "Default role for authenticated end users",
            ["USER_READ"], ct);

        // 3. ROLE_ADMIN — full control
        var allPermNames = new List<string>(DefaultPermissions.Length);
        foreach (var (resource, action, _) in DefaultPermissions)
            allPermNames.Add(PermissionName.Of(resource, action).Value);

        var adminRole = await EnsureRoleAsync("ROLE_ADMIN", "Administrator with full system access", allPermNames, ct);

        // 4. Default Admin Account — admin / Admin@123456
        await EnsureAdminUserAsync(adminRole.Id, ct);
    }

    private async Task EnsureAdminUserAsync(RoleId adminRoleId, CancellationToken ct)
    {
        const string adminUsername = "admin";
        const string adminEmail = "admin@example.com";
        const string adminPass = "Admin@123456";

        var existingUser = await userRepository.FindByUsernameAsync(Username.Of(adminUsername), ct);
        if (existingUser is null)
        {
            var user = User.Register(
                Username.Of(adminUsername),
                Email.Of(adminEmail),
                Password.CreateRaw(adminPass),
                PersonName.Of("System", "Administrator"),
                passwordEncoder);

            user.AssignRole(adminRoleId);
            await userRepository.SaveAsync(user, ct);
            logger.LogInformation("[seed] Inserted default admin account: username={Username}", adminUsername);
        }
        else if (!existingUser.Roles.Contains(adminRoleId))
        {
            existingUser.AssignRole(adminRoleId);
            await userRepository.SaveAsync(existingUser, ct);
            logger.LogInformation("[seed] Assigned ROLE_ADMIN to existing user {Username}", adminUsername);
        }
    }

    private async Task<Role> EnsureRoleAsync(
        string roleNameRaw, string description, IReadOnlyList<string> permissionNames, CancellationToken ct)
    {
        var roleName = RoleName.Of(roleNameRaw);
        var role = await roleRepository.FindByNameAsync(roleName, ct);

        if (role is null)
        {
            role = Role.Create(roleName, description);
            logger.LogInformation("[seed] Inserted role {Name}", role.Name.Value);
        }

        foreach (var permName in permissionNames)
        {
            var perm = await permissionRepository.FindByNameAsync(PermissionName.FromString(permName), ct);
            if (perm is not null && !role.HasPermission(perm.Id))
            {
                role.GrantPermission(perm.Id);
            }
        }

        return await roleRepository.SaveAsync(role, ct);
    }
}
