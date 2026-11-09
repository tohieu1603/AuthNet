namespace CommonLib.Error;

using System.Text.Json.Serialization;

/// <summary>
/// Single validation violation on a specific request field.
/// </summary>
public sealed record FieldError(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("rejectedValue"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] object? RejectedValue = null
);

/// <summary>
/// Machine-readable error payload returned by every service's GlobalExceptionHandler/Middleware.
/// Mirrors ApiResponse shape so successful and failed responses can be parsed predictably.
/// </summary>
public sealed record ErrorResponse(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("path"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Path = null,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp = default,
    [property: JsonPropertyName("fieldErrors"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<FieldError>? FieldErrors = null,
    [property: JsonPropertyName("traceId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId = null,
    [property: JsonPropertyName("details"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, object>? Details = null
)
{
    /// <summary>Compact factory for typical error responses.</summary>
    public static ErrorResponse Of(string code, string? message = null, string? path = null) =>
        new(code, message ?? code, path, DateTimeOffset.UtcNow, null, null, null);

    /// <summary>Factory for validation failures.</summary>
    public static ErrorResponse Validation(string? path, IReadOnlyList<FieldError> fieldErrors) =>
        new(ErrorCode.ValidationFailed, "Request validation failed", path, DateTimeOffset.UtcNow, fieldErrors, null, null);

    /// <summary>Attaches a trace id to an existing error response.</summary>
    public ErrorResponse WithTraceId(string traceId) =>
        this with { TraceId = traceId };
}
