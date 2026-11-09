using System.Text.Json.Serialization;

namespace AuthService.Application.DTOs;

/// <summary>
/// Read model of a User aggregate.
/// </summary>
public sealed record UserDto(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Enabled,
    bool AccountNonExpired,
    bool AccountNonLocked,
    bool CredentialsNonExpired,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? LastLogin
);

/// <summary>
/// Read model for a role.
/// </summary>
public sealed record RoleDto(
    string Id,
    string Name,
    string? Description,
    IReadOnlySet<string> Permissions,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>
/// Read model for a permission.
/// </summary>
public sealed record PermissionDto(
    string Id,
    string Name,
    string Resource,
    string Action,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

/// <summary>
/// Generic pagination envelope.
/// </summary>
public sealed record PageDto<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    int PageSize,
    long TotalElements
)
{
    public static PageDto<T> Of(IReadOnlyList<T> items, string? nextCursor, int pageSize, long totalElements) =>
        new(items, nextCursor, pageSize, totalElements);
}

/// <summary>
/// Unified envelope for REST response.
/// </summary>
public sealed record ApiResponse<T>(
    bool Success,
    string Code,
    string? Message,
    T? Data,
    DateTimeOffset Timestamp,
    string? TraceId
)
{
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, "OK", message, data, DateTimeOffset.UtcNow, null);

    public static ApiResponse<T> Error(string code, string message) =>
        new(false, code, message, default, DateTimeOffset.UtcNow, null);

    public ApiResponse<T> WithTraceId(string traceId) =>
        this with { TraceId = traceId };
}

public sealed record FieldErrorDto(string Field, string Message, object? RejectedValue);

/// <summary>
/// Machine-readable error payload returned by GlobalExceptionHandler.
/// </summary>
public sealed record ErrorResponseDto(
    string Code,
    string Message,
    string? Path,
    DateTimeOffset Timestamp,
    IReadOnlyList<FieldErrorDto>? FieldErrors,
    string? TraceId,
    IReadOnlyDictionary<string, object>? Details
)
{
    public static ErrorResponseDto Of(string code, string message, string? path = null) =>
        new(code, message, path, DateTimeOffset.UtcNow, null, null, null);

    public static ErrorResponseDto Validation(string? path, IReadOnlyList<FieldErrorDto> fieldErrors) =>
        new("COMMON-400", "Request validation failed", path, DateTimeOffset.UtcNow, fieldErrors, null, null);
}
