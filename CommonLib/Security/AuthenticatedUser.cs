namespace CommonLib.Security;

/// <summary>
/// Authenticated user context extracted from a JWT and used as the principal
/// by downstream services (non-auth-service). Produced by <see cref="JwtTokenValidator"/>.
/// </summary>
public sealed record AuthenticatedUser(
    string UserId,
    string Username,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions
)
{
    public static AuthenticatedUser Of(string userId, string username, IReadOnlyList<string> roles) =>
        new(userId, username, roles ?? Array.Empty<string>(), Array.Empty<string>());

    public bool HasRole(string role) =>
        Roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool HasAnyRole(params string[] candidates) =>
        candidates.Any(r => Roles.Contains(r, StringComparer.OrdinalIgnoreCase));

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);

    public bool HasAnyPermission(params string[] candidates) =>
        candidates.Any(p => Permissions.Contains(p, StringComparer.OrdinalIgnoreCase));
}
