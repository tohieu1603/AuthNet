using AuthService.Application.Ports;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AuthService.Infrastructure.Security;

public class TokenBlacklistService(
    IConnectionMultiplexer? redisConnection,
    AuthDbContext dbContext,
    ILogger<TokenBlacklistService> logger
) : ITokenBlacklistPort
{
    private const string BlacklistPrefix = "blacklist:";

    public async Task RevokeAsync(
        string tokenId,
        string userId,
        DateTimeOffset expiresAt,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl > TimeSpan.Zero && redisConnection != null && redisConnection.IsConnected)
        {
            try
            {
                var db = redisConnection.GetDatabase();
                await db.StringSetAsync(BlacklistPrefix + tokenId, reason, ttl);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis unavailable for blacklist write; falling back to DB.");
            }
        }

        var entity = new TokenRevocationEntity
        {
            Id = tokenId,
            UserId = userId,
            RevokedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
            Reason = reason,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var existing = await dbContext.TokenRevocations.FindAsync([tokenId], cancellationToken);
        if (existing == null)
        {
            await dbContext.TokenRevocations.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (redisConnection != null && redisConnection.IsConnected)
        {
            try
            {
                var db = redisConnection.GetDatabase();
                if (await db.KeyExistsAsync(BlacklistPrefix + tokenId))
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis unavailable for blacklist check; falling back to DB.");
            }
        }

        return await dbContext.TokenRevocations
            .AsNoTracking()
            .AnyAsync(r => r.Id == tokenId, cancellationToken);
    }

    public async Task WarmUpRedisFromDbAsync(CancellationToken cancellationToken = default)
    {
        if (redisConnection == null || !redisConnection.IsConnected) return;

        try
        {
            var now = DateTimeOffset.UtcNow;
            var active = await dbContext.TokenRevocations
                .AsNoTracking()
                .Where(r => r.ExpiresAt > now)
                .ToListAsync(cancellationToken);

            var db = redisConnection.GetDatabase();
            foreach (var row in active)
            {
                var ttl = row.ExpiresAt - DateTimeOffset.UtcNow;
                if (ttl > TimeSpan.Zero)
                {
                    await db.StringSetAsync(BlacklistPrefix + row.Id, row.Reason, ttl);
                }
            }
            logger.LogInformation("Blacklist warm-up: restored {Count} active revocation(s) into Redis", active.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Blacklist warm-up failed or skipped.");
        }
    }

    public async Task CleanupExpiredRevocationsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        int deleted = await dbContext.TokenRevocations
            .Where(r => r.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogDebug("Purged {Count} expired token revocation rows", deleted);
    }
}
