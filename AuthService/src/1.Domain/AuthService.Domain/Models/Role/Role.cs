using AuthService.Domain.Models.Permission.VO;
using AuthService.Domain.Models.Role.Events;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.Role;

public sealed class Role : AggregateRoot, IEquatable<Role>
{
    private readonly HashSet<PermissionId> _permissions = [];

    public RoleId Id { get; private init; }
    public RoleName Name { get; private set; }
    public string? Description { get; private set; }
    public IReadOnlySet<PermissionId> Permissions => _permissions;
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Role() { }

    private Role(RoleId id, RoleName name, string? description, IEnumerable<PermissionId>? permissions, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        Description = description;
        _permissions = permissions is null ? [] : new HashSet<PermissionId>(permissions);
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Role Create(RoleName name, string? description)
    {
        var now = DateTimeOffset.UtcNow;
        return new Role(
            RoleId.Generate(),
            name,
            description,
            permissions: null,
            now,
            now
        );
    }

    public static Role Reconstitute(RoleId id, RoleName name, string? description, IEnumerable<PermissionId>? permissions, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        return new Role(id, name, description, permissions, createdAt, updatedAt);
    }
    public void UpdateDescription(string? description)
    {
        if(string.Equals(Description, description, StringComparison.Ordinal)) return;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void GrantPermission(PermissionId permission)
    {
        if(_permissions.Add(permission))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            RegisterEvent(new PermissionGrantedEvent(Id.Value, permission.Value));
        }
    }
    public void RevokePermission(PermissionId permission)
    {
        if(_permissions.Remove(permission))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            RegisterEvent(new PermissionRevokedEvent(Id.Value, permission.Value));
        }
    }
    public bool HasPermission(PermissionId permission) => _permissions.Contains(permission);
    public bool Equals(Role? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Role);
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(Role? left, Role? right) => Equals(left, right);
    public static bool operator !=(Role? left, Role? right) => !Equals(left, right);
    public override string ToString() => $"Role [Id={Id}, Name={Name}, Description={Description}]";
}