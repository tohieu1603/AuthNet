using AuthService.Application.Ports;
using AuthService.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Cache;

public class RedisRolePermissionCacheAdapter(
    IConnectionMultiplexer? redisConnection,
    IServiceProvider serviceProvider,
    ILogger<RedisRolePermissionCacheAdapter> logger
) : IRolePermissionCachePort
{
    private const string KeyPrefix = "role_permissions:";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlySet<string>?> GetAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (redisConnection == null || !redisConnection.IsConnected) return null;

        try
        {
            var db = redisConnection.GetDatabase();
            var members = await db.SetMembersAsync(KeyPrefix + roleName);
            if (members == null || members.Length == 0) return null;

            return members.Select(m => m.ToString()).ToHashSet();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for role cache read ({RoleName})", roleName);
            return null;
        }
    }

    public async Task PutAsync(string roleName, IEnumerable<string> permissionNames, CancellationToken cancellationToken = default)
    {
        if (redisConnection == null || !redisConnection.IsConnected) return;

        var perms = permissionNames.ToList();
        if (perms.Count == 0)
        {
            await EvictAsync(roleName, cancellationToken);
            return;
        }

        try
        {
            var db = redisConnection.GetDatabase();
            string key = KeyPrefix + roleName;
            await db.KeyDeleteAsync(key);

            var redisValues = perms.Select(p => (RedisValue)p).ToArray();
            await db.SetAddAsync(key, redisValues);
            await db.KeyExpireAsync(key, Ttl);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for role cache write ({RoleName})", roleName);
        }
    }

    public async Task EvictAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (redisConnection == null || !redisConnection.IsConnected) return;

        try
        {
            var db = redisConnection.GetDatabase();
            await db.KeyDeleteAsync(KeyPrefix + roleName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for role cache evict ({RoleName})", roleName);
        }
    }

    public async Task EvictAllAsync(CancellationToken cancellationToken = default)
    {
        if (redisConnection == null || !redisConnection.IsConnected) return;

        try
        {
            var endpoints = redisConnection.GetEndPoints();
            foreach (var endpoint in endpoints)
            {
                var server = redisConnection.GetServer(endpoint);
                if (server.IsConnected)
                {
                    var keys = server.Keys(pattern: KeyPrefix + "*").ToArray();
                    if (keys.Length > 0)
                    {
                        var db = redisConnection.GetDatabase();
                        await db.KeyDeleteAsync(keys);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for role cache wipe");
        }
    }

    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        if (redisConnection == null || !redisConnection.IsConnected) return;

        try
        {
            var roleRepository = serviceProvider.GetRequiredService<IRoleRepository>();
            var permissionRepository = serviceProvider.GetRequiredService<IPermissionRepository>();
            int count = 0;
            var roles = await roleRepository.FindAllAsync(cancellationToken);
            foreach (var role in roles)
            {
                var permNames = new HashSet<string>();
                if (role.Permissions.Count > 0)
                {
                    var perms = await permissionRepository.FindByIdInAsync(role.Permissions, cancellationToken);
                    permNames = perms.Select(p => p.Name.Value).ToHashSet();
                }

                await PutAsync(role.Name.Value, permNames, cancellationToken);
                count++;
            }
            logger.LogInformation("Role-permission cache warm-up: populated {Count} role(s) into Redis", count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Role-permission cache warm-up failed");
        }
    }
}
