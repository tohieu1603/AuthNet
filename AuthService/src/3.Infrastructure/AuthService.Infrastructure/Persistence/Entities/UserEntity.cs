using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity mapped to the <c>users</c> PostgreSQL table.
/// </summary>
[Table("users")]
public sealed class UserEntity
{
    [Key, Column("id"), MaxLength(36)]
    public string Id { get; set; } = string.Empty;

    [Column("username"), MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Column("email"), MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Column("password"), MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [Column("first_name"), MaxLength(64)]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name"), MaxLength(64)]
    public string LastName { get; set; } = string.Empty;

    [Column("enabled")]
    public bool Enabled { get; set; } = true;

    [Column("account_non_expired")]
    public bool AccountNonExpired { get; set; } = true;

    [Column("account_non_locked")]
    public bool AccountNonLocked { get; set; } = true;

    [Column("credentials_non_expired")]
    public bool CredentialsNonExpired { get; set; } = true;

    [Column("last_login")]
    public DateTimeOffset? LastLogin { get; set; }

    [Column("token_version")]
    public int TokenVersion { get; set; } = 1;

    [Column("google_sub"), MaxLength(255)]
    public string? GoogleSub { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<RoleEntity> Roles { get; set; } = [];
}
