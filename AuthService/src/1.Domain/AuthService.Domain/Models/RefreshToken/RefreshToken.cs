using AuthService.Domain.Models.RefreshToken.Events;
using AuthService.Domain.Models.RefreshToken.Exceptions;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.RefreshToken;

/// <summary>
/// RefreshToken aggregate root.
/// Implements Refresh-Token Rotation + Family Revocation pattern.
/// </summary>
public sealed class RefreshToken : AggregateRoot, IEquatable<RefreshToken>
{
    public TokenId Id { get; private init; }
    public TokenValue Value { get; private init; }
    public TokenFamily Family { get; private init; }
    public GenerationNumber Generation { get; private init; }
    public TokenExpiry Expiry { get; private init; }
    public UserId UserId { get; private init; }
    public bool Revoked { get; private set; }
    public RevokedReason? Reason { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset? RevokedAt { get; private set; }

    private RefreshToken()
    {
        Revoked = false;
    }

    private RefreshToken(
        TokenId id,
        TokenValue value,
        UserId userId,
        TokenFamily family,
        GenerationNumber generation,
        TokenExpiry expiry,
        bool revoked,
        RevokedReason? reason,
        DateTimeOffset createdAt,
        DateTimeOffset? revokedAt)
    {
        Id = id;
        Value = value;
        UserId = userId;
        Family = family;
        Generation = generation;
        Expiry = expiry;
        Revoked = revoked;
        Reason = reason;
        CreatedAt = createdAt;
        RevokedAt = revokedAt;
    }

    public static RefreshToken Create(UserId userId, int expiryDays)
    {
        var now = DateTimeOffset.UtcNow;
        var token = new RefreshToken
        {
            Id = TokenId.Generate(),
            Value = TokenValue.Generate(),
            UserId = userId,
            Family = TokenFamily.Generate(),
            Generation = GenerationNumber.Root(),
            Expiry = TokenExpiry.FromDaysFromNow(expiryDays),
            Revoked = false,
            CreatedAt = now,
        };

        token.RegisterEvent(new TokenCreatedEvent(
            token.Id.Value,
            userId.Value,
            token.Family.Value,
            token.Generation.Value,
            token.Expiry.Value));

        return token;
    }

    public static RefreshToken Rotate(RefreshToken oldToken, int expiryDays)
    {
        var now = DateTimeOffset.UtcNow;
        var newToken = new RefreshToken
        {
            Id = TokenId.Generate(),
            Value = TokenValue.Generate(),
            UserId = oldToken.UserId,
            Family = oldToken.Family,
            Generation = oldToken.Generation.Next(),
            Expiry = TokenExpiry.FromDaysFromNow(expiryDays),
            Revoked = false,
            CreatedAt = now,
        };

        newToken.RegisterEvent(new TokenRotatedEvent(
            oldToken.UserId.Value,
            oldToken.Family.Value,
            oldToken.Id.Value,
            newToken.Id.Value,
            newToken.Generation.Value));

        return newToken;
    }

    public static RefreshToken Reconstitute(
        TokenId id,
        TokenValue value,
        UserId userId,
        TokenExpiry expiry,
        bool revoked,
        DateTimeOffset createdAt,
        DateTimeOffset? revokedAt,
        TokenFamily family,
        GenerationNumber generation,
        RevokedReason? reason)
    {
        return new RefreshToken(
            id, value, userId, family, generation, expiry, revoked, reason, createdAt, revokedAt);
    }

    public void VerifyValidity()
    {
        if (Revoked)
        {
            throw new TokenRevokedException(Id.Value, Reason?.Value ?? "UNKNOWN");
        }
        if (Expiry.IsExpired())
        {
            throw new TokenExpiredException(Id.Value);
        }
    }

    public bool IsValid => !Revoked && !Expiry.IsExpired();
    public bool IsRevoked => Revoked;
    public bool IsReuseAttempt => Revoked;
    public bool BelongsTo(UserId u) => UserId.Equals(u);
    public bool WillExpireSoon(long seconds) => Expiry.WillExpireWithin(seconds);
    public long GetRemainingSeconds() => IsValid ? Expiry.RemainingSeconds : 0L;

    public bool WasRevokedForSecurity() =>
        Reason.HasValue && Reason.Value.IsSecurityRelated();

    public void Revoke(RevokedReason reason)
    {
        if (Revoked) return;
        Revoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        Reason = reason;

        RegisterEvent(new TokenRevokedEvent(
            Id.Value,
            UserId.Value,
            Family.Value,
            reason.Value));
    }

    public void Revoke() => Revoke(RevokedReason.Normal);

    public bool Equals(RefreshToken? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as RefreshToken);
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(RefreshToken? left, RefreshToken? right) => Equals(left, right);
    public static bool operator !=(RefreshToken? left, RefreshToken? right) => !Equals(left, right);
    public override string ToString() => $"RefreshToken [Id={Id}, UserId={UserId}, Family={Family}, Generation={Generation}]";
}