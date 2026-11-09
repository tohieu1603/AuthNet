using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuthService.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core entity mapped to the <c>permissions</c> PostgreSQL table.
/// </summary>
[Table("permissions")]
public sealed class PermissionEntity
{
    [Key, Column("id"), MaxLength(36)]
    public string Id { get; set; } = string.Empty;

    [Column("name"), MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("resource"), MaxLength(50)]
    public string? Resource { get; set; }

    [Column("action"), MaxLength(50)]
    public string? Action { get; set; }

    [Column("description"), MaxLength(255)]
    public string? Description { get; set; }

    [Column("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<RoleEntity> Roles { get; set; } = [];
}
