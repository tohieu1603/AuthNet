using AuthService.Domain.Models.RefreshToken;
using AuthService.Domain.Models.RefreshToken.Exceptions;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;

namespace AuthService.Domain.Services;

/// <summary>
/// Cross-aggregate token business rules: issue, rotate, reuse-detection, family revocation.
/// </summary>
public class TokenDomainService
{
    public RefreshToken IssueForUser(User user, int expiryDays)
    {
        user.EnsureAuthenticatable();
        return RefreshToken.Create(user.Id, expiryDays);
    }

    public async Task<RefreshToken> RotateAsync(
        RefreshToken oldToken,
        int expiryDays,
        IRefreshTokenRepository repository,
        CancellationToken cancellationToken = default)
    {
        if (oldToken.IsReuseAttempt)
        {
            await RevokeFamilyAsync(oldToken.Family, repository, cancellationToken);
            throw new TokenReuseDetectedException(oldToken.Family.Value, oldToken.Generation.Value);
        }

        oldToken.VerifyValidity();
        oldToken.Revoke(RevokedReason.Normal);
        await repository.SaveAsync(oldToken, cancellationToken);

        return RefreshToken.Rotate(oldToken, expiryDays);
    }

    public async Task RevokeFamilyAsync(
        TokenFamily family,
        IRefreshTokenRepository repository,
        CancellationToken cancellationToken = default)
    {
        var tokens = await repository.FindByFamilyAsync(family, cancellationToken);
        foreach (var t in tokens)
        {
            if (!t.IsRevoked)
            {
                t.Revoke(RevokedReason.FamilyRevoked);
                await repository.SaveAsync(t, cancellationToken);
            }
        }
    }

    public void ValidateOwnership(RefreshToken token, UserId userId)
    {
        if (!token.BelongsTo(userId))
        {
            throw new TokenOwnershipException(userId.Value);
        }
    }
}
