using AuthService.Domain.Models.RefreshToken;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Models.User.VO;

namespace AuthService.Domain.Repositories;

/// <summary>
/// Domain Repository Interface for RefreshToken Aggregate
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken> SaveAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindByIdAsync(TokenId tokenId, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindByTokenValueAsync(TokenValue tokenValue, CancellationToken cancellationToken = default);
    Task<RefreshToken?> FindByTokenValueForUpdateAsync(TokenValue tokenValue, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> FindByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> FindValidTokensByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> FindByFamilyAsync(TokenFamily family, CancellationToken cancellationToken = default);
    Task DeleteAsync(RefreshToken token, CancellationToken cancellationToken = default);
    Task DeleteByUserIdAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<int> DeleteExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task RevokeAllTokensForUserAsync(UserId userId, CancellationToken cancellationToken = default);
}
