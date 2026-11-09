using Microsoft.AspNetCore.Authorization;

namespace AuthService.Api.Security;

/// <summary>
/// Attribute to enforce fine-grained permissions on Controllers or Actions.
/// Example:
/// <code>
/// [Authorize(Roles = "ROLE_ADMIN")]
/// [RequirePermission("USER_READ")]
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(string permission)
    : AuthorizeAttribute(policy: PolicyPrefix + permission)
{
    public const string PolicyPrefix = "perm:";
    public string Permission => permission;
}

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
