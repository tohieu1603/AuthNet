using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity mapped to the <c>token_revocations</c> PostgreSQL table.
/// Stores blacklisted access-token JTIs with their expiry for DB-fallback lookups.
/// </summary>
[Table("token_revocations")]
public sealed class TokenRevocationEntity
{
    /// <summary>JWT <c>jti</c> claim — the unique token identifier.</summary>
    [Key, Column("id"), MaxLength(36)]
    public string Id { get; set; } = string.Empty;

    [Column("user_id"), MaxLength(36)]
    public string UserId { get; set; } = string.Empty;

    [Column("revoked_at")]
    public DateTimeOffset RevokedAt { get; set; }

    [Column("expires_at")]
    public DateTimeOffset ExpiresAt { get; set; }

    [Column("reason"), MaxLength(50)]
    public string Reason { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
