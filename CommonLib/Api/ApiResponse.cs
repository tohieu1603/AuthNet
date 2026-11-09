namespace CommonLib.Api;

using System.Text.Json.Serialization;

/// <summary>
/// Unified envelope for every REST response in the platform.
/// Null fields are omitted from the wire format so successful responses don't carry empty error slots.
/// </summary>
public sealed record ApiResponse<T>(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message,
    [property: JsonPropertyName("data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] T? Data,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp,
    [property: JsonPropertyName("traceId"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? TraceId
)
{
    /// <summary>Success response with only a payload.</summary>
    public static ApiResponse<T> Ok(T data) =>
        new(true, "OK", null, data, DateTimeOffset.UtcNow, null);

    /// <summary>Success response with a human-readable message + payload.</summary>
    public static ApiResponse<T> Ok(T data, string? message) =>
        new(true, "OK", message, data, DateTimeOffset.UtcNow, null);

    /// <summary>Error response — pair with <see cref="CommonLib.Error.ErrorCode"/>.</summary>
    public static ApiResponse<T> Error(string code, string message) =>
        new(false, code, message, default, DateTimeOffset.UtcNow, null);

    /// <summary>Attaches a trace id to an existing response.</summary>
    public ApiResponse<T> WithTraceId(string traceId) =>
        this with { TraceId = traceId };
}
