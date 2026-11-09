using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity mapped to the <c>roles</c> PostgreSQL table.
/// </summary>
[Table("roles")]
public sealed class RoleEntity
{
    [Key, Column("id"), MaxLength(36)]
    public string Id { get; set; } = string.Empty;

    [Column("name"), MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Column("description"), MaxLength(255)]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<PermissionEntity> Permissions { get; set; } = [];
    public ICollection<UserEntity> Users { get; set; } = [];
}
