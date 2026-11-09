using StackExchange.Redis;

namespace ApiGateway.Security;

/// <summary>
/// Service to verify if a token JTI has been revoked / blacklisted in Redis.
/// Direct port of Java TokenRevocationFilter with Fail-Open policy.
/// Key pattern: "blacklist:{jti}"
/// </summary>
public interface ITokenRevocationValidator
{
    Task<bool> IsRevokedAsync(string jti);
}

public sealed class RedisTokenRevocationValidator(
    IConnectionMultiplexer? redisConnection,
    ILogger<RedisTokenRevocationValidator> logger) : ITokenRevocationValidator
{
    private const string BlacklistKeyPrefix = "blacklist:";

    public async Task<bool> IsRevokedAsync(string jti)
    {
        if (string.IsNullOrWhiteSpace(jti) || redisConnection is null || !redisConnection.IsConnected)
        {
            // Fail-open policy if Redis is down
            return false;
        }

        try
        {
            var db = redisConnection.GetDatabase();
            return await db.KeyExistsAsync(BlacklistKeyPrefix + jti);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for blacklist check (jti={Jti}); failing open", jti);
            return false;
        }
    }
}
