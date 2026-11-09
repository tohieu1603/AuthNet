using System.ComponentModel.DataAnnotations;

namespace AuthService.Api.DTOs;

/// <summary>POST /api/v1/auth/register</summary>
public sealed record RegisterRequest(
    [Required, MinLength(3), MaxLength(50)] string Username,
    [Required, EmailAddress, MaxLength(100)] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    [Required, MaxLength(64)] string FirstName,
    [Required, MaxLength(64)] string LastName
);

/// <summary>POST /api/v1/auth/login</summary>
public sealed record LoginRequest(
    [Required] string UsernameOrEmail,
    [Required] string Password
);

/// <summary>POST /api/v1/auth/google</summary>
public sealed record GoogleLoginRequest(
    [Required] string IdToken
);

/// <summary>POST /api/v1/auth/refresh — optional body fallback for non-cookie clients.</summary>
public sealed record RefreshRequest(
    string? RefreshToken
);

/// <summary>POST /api/v1/auth/change-password</summary>
public sealed record ChangePasswordRequest(
    [Required] string OldPassword,
    [Required, MinLength(8), MaxLength(128)] string NewPassword
);

/// <summary>PATCH /api/v1/users/me/email</summary>
public sealed record UpdateEmailRequest(
    [Required, EmailAddress, MaxLength(100)] string NewEmail
);

/// <summary>POST /api/v1/users/{userId}/roles</summary>
public sealed record RoleAssignmentRequest(
    [Required, MaxLength(50)] string RoleName
);

/// <summary>GET /api/v1/auth/me — safe projection of the authenticated principal.</summary>
public sealed record AuthMeResponse(
    string UserId,
    string Username,
    string Email,
    IReadOnlyCollection<string> Roles,
    int TokenVersion
);
