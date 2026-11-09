using AuthService.Application.Ports;
using AuthService.Domain.Events;
using AuthService.Domain.Models.Role.Events;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Messaging;

public class RolePermissionCacheInvalidator(
    IServiceProvider serviceProvider,
    IPermissionRepository permissionRepository,
    IRolePermissionCachePort cache,
    ILogger<RolePermissionCacheInvalidator> logger
)
{
    public async Task HandleDomainEventAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent is PermissionGrantedEvent or PermissionRevokedEvent)
        {
            await RefreshAsync(domainEvent.AggregateId, cancellationToken);
        }
    }

    private async Task RefreshAsync(string roleIdRaw, CancellationToken cancellationToken)
    {
        try
        {
            var roleRepository = serviceProvider.GetRequiredService<IRoleRepository>();
            var role = await roleRepository.FindByIdAsync(RoleId.Of(roleIdRaw), cancellationToken);
            if (role == null)
            {
                logger.LogDebug("Role {RoleId} no longer exists; leaving cache as-is", roleIdRaw);
                return;
            }

            var permNames = new HashSet<string>();
            if (role.Permissions.Count > 0)
            {
                var perms = await permissionRepository.FindByIdInAsync(role.Permissions, cancellationToken);
                permNames = perms.Select(p => p.Name.Value).ToHashSet();
            }

            await cache.PutAsync(role.Name.Value, permNames, cancellationToken);
            logger.LogInformation("Role-permission cache refreshed for role={RoleName} ({Count} perms)",
                role.Name.Value, permNames.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to refresh role-permission cache for {RoleId}; evicting instead.", roleIdRaw);
            await cache.EvictAllAsync(cancellationToken);
        }
    }
}
