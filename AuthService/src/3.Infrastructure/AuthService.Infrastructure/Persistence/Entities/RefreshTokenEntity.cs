using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity mapped to the <c>refresh_tokens</c> PostgreSQL table.
/// Implements refresh-token rotation with family-based revocation.
/// </summary>
[Table("refresh_tokens")]
public sealed class RefreshTokenEntity
{
    [Key, Column("id"), MaxLength(36)]
    public string Id { get; set; } = string.Empty;

    [Column("token"), MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [Column("user_id"), MaxLength(36)]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public UserEntity? User { get; set; }

    [Column("expiry_date")]
    public DateTimeOffset ExpiryDate { get; set; }

    [Column("revoked")]
    public bool Revoked { get; set; }

    [Column("revoked_at")]
    public DateTimeOffset? RevokedAt { get; set; }

    [Column("family"), MaxLength(100)]
    public string Family { get; set; } = string.Empty;

    [Column("generation")]
    public int Generation { get; set; }

    [Column("revoked_reason"), MaxLength(50)]
    public string? RevokedReason { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }
}
