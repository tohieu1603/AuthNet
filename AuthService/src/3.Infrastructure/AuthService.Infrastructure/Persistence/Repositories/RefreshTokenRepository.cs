using AuthService.Domain.Events;
using AuthService.Domain.Models.RefreshToken;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Persistence.Mappers;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository(
    AuthDbContext dbContext,
    RefreshTokenHasher tokenHasher,
    IDomainEventPublisher eventPublisher
) : IRefreshTokenRepository
{
    private string ToDigest(TokenValue tokenValue) => tokenHasher.Hash(tokenValue.Value);

    public async Task<RefreshToken> SaveAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == token.Id.Value, cancellationToken);

        if (existing == null)
        {
            string digest = ToDigest(token.Value);
            var entity = EntityMappers.ToEntity(token, digest);
            await dbContext.RefreshTokens.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            existing.Revoked = token.Revoked;
            existing.RevokedAt = token.RevokedAt;
            existing.RevokedReason = token.Reason?.Value;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var events = token.PullDomainEvents();
        await eventPublisher.PublishAllAsync(events, cancellationToken);

        return token;
    }

    public async Task<RefreshToken?> FindByIdAsync(TokenId tokenId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<RefreshToken?> FindByTokenValueAsync(TokenValue tokenValue, CancellationToken cancellationToken = default)
    {
        string digest = ToDigest(tokenValue);
        var entity = await dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == digest, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<RefreshToken?> FindByTokenValueForUpdateAsync(TokenValue tokenValue, CancellationToken cancellationToken = default)
    {
        string digest = ToDigest(tokenValue);

        // PostgreSQL row-level pessimistic locking
        var entity = await dbContext.RefreshTokens
            .FromSqlInterpolated($"SELECT * FROM refresh_tokens WHERE token = {digest} FOR UPDATE")
            .FirstOrDefaultAsync(cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId.Value)
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<RefreshToken>> FindValidTokensByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entities = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId.Value && !t.Revoked && t.ExpiryDate > now)
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<RefreshToken>> FindByFamilyAsync(TokenFamily family, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.Family == family.Value)
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task DeleteAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == token.Id.Value, cancellationToken);

        if (entity != null)
        {
            dbContext.RefreshTokens.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task DeleteByUserIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId.Value)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await dbContext.RefreshTokens
            .Where(t => t.ExpiryDate <= now)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task RevokeAllTokensForUserAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        await dbContext.RefreshTokens
            .Where(t => t.UserId == userId.Value && !t.Revoked)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Revoked, true)
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.RevokedReason, "USER_REVOKED"),
                cancellationToken);
    }
}
