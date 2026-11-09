namespace AuthService.Domain.Models.Permission.VO;

public readonly record struct PermissionName
{
    public string Resource { get; }
    public string Action { get; }
    public string Value { get; }

    public PermissionName(string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Permission resource cannot be empty.", nameof(resource));
        }
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Permission action cannot be empty.", nameof(action));
        }

        Resource = resource.Trim().ToUpperInvariant();
        Action = action.Trim().ToUpperInvariant();
        Value = $"{Resource}_{Action}";
    }

    public static PermissionName Of(string resource, string action) => new(resource, action);

    /// <summary>
    /// Create from format string (e.g. "USER_READ")
    /// </summary>
    public static PermissionName FromString(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            throw new ArgumentException("Permission name cannot be empty.", nameof(permission));
        }

        var trimmed = permission.Trim().ToUpperInvariant();
        var indexOfUnderScore = trimmed.IndexOf('_');

        if (indexOfUnderScore < 0 || indexOfUnderScore != trimmed.LastIndexOf('_') || indexOfUnderScore == trimmed.Length - 1)
        {
            throw new ArgumentException($"Invalid permission format. Expected: RESOURCE_ACTION, got: {permission}", nameof(permission));
        }
        var resource = trimmed[..indexOfUnderScore];
        var action = trimmed[(indexOfUnderScore + 1)..];

        return new(resource, action);
    }

    // Retain legacy method name as alias
    public static PermissionName FormString(string permission) => FromString(permission);

    /// <summary>
    /// Check if this permission applies to a specific resource
    /// </summary>
    public bool IsForResource(string? resource) =>
        string.Equals(Resource, resource?.Trim(), StringComparison.OrdinalIgnoreCase);

    public bool AllowsAction(string? action) =>
        string.Equals(Action, action?.Trim(), StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}