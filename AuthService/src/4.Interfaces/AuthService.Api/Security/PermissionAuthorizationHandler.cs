using System.Security.Claims;
using AuthService.Application.Ports;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace AuthService.Api.Security;

/// <summary>
/// Evaluates whether the current principal holds the required permission,
/// checking JWT claims first, then falling back to cached role permissions in Redis / Repository.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IRolePermissionCachePort rolePermissionCache,
    IRoleRepository roleRepository,
    IPermissionRepository permissionRepository,
    ILogger<PermissionAuthorizationHandler> logger
) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1. Direct claim on principal: "permission" or "perm"
        if (context.User.HasClaim("permission", requirement.Permission) ||
            context.User.HasClaim("perm", requirement.Permission))
        {
            context.Succeed(requirement);
            return;
        }

        // 2. Roles claim check via Redis cache + DB fallback
        var roles = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roles.Count == 0)
        {
            return;
        }

        foreach (var roleName in roles)
        {
            var cachedPerms = await rolePermissionCache.GetAsync(roleName);
            if (cachedPerms is not null)
            {
                if (cachedPerms.Contains(requirement.Permission))
                {
                    context.Succeed(requirement);
                    return;
                }
                continue;
            }

            // Cache miss: look up role permissions from DB and populate cache
            try
            {
                var role = await roleRepository.FindByNameAsync(RoleName.Of(roleName));
                if (role is not null && role.Permissions.Count > 0)
                {
                    var perms = await permissionRepository.FindByIdInAsync(role.Permissions);
                    var permNames = perms.Select(p => p.Name.Value).ToHashSet();

                    await rolePermissionCache.PutAsync(roleName, permNames);

                    if (permNames.Contains(requirement.Permission))
                    {
                        context.Succeed(requirement);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to resolve permissions for role {RoleName}", roleName);
            }
        }
    }
}
