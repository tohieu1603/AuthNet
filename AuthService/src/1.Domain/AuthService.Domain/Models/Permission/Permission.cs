using AuthService.Domain.Models.Permission.VO;

namespace AuthService.Domain.Models.Permission;

/// <summary>
/// Permission Entity (part of Role Aggregate)
/// Represents a specific permission that can be granted to roles
/// </summary>
public class Permission : IEquatable<Permission>
{
    public PermissionId Id { get; private init; }
    public PermissionName Name { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Permission() { }

    private Permission(PermissionId id, PermissionName name, string? description, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Permission Create(string resource, string action, string? description)
    {
        var now = DateTimeOffset.UtcNow;
        return new Permission(
            PermissionId.Generate(),
            PermissionName.Of(resource, action),
            description,
            now,
            now
        );
    }

    public static Permission Reconstitute(PermissionId id, string resource, string action, string? description, DateTimeOffset createdAt, DateTimeOffset updatedAt)
    {
        return new Permission(
            id,
            PermissionName.Of(resource, action),
            description,
            createdAt,
            updatedAt
        );
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Check if this permission grants access to a specific resource and action
    /// </summary>
    public bool Grants(string resource, string action) =>
        Name.IsForResource(resource) && Name.AllowsAction(action);

    public bool Equals(Permission? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Permission);
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(Permission? left, Permission? right) => Equals(left, right);
    public static bool operator !=(Permission? left, Permission? right) => !Equals(left, right);
    public override string ToString() => $"Permission [Id={Id}, Name={Name}, Description={Description}]";
}