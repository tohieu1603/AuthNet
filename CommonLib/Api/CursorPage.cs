namespace CommonLib.Api;

using System.Text.Json.Serialization;

/// <summary>
/// Cursor-based pagination envelope shared across services.
/// Gives stable O(log n) lookup regardless of page depth and tolerates concurrent inserts.
/// </summary>
public sealed record CursorPage<T>(
    [property: JsonPropertyName("items")] IReadOnlyList<T> Items,
    [property: JsonPropertyName("nextCursor"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? NextCursor,
    [property: JsonPropertyName("size")] int Size,
    [property: JsonPropertyName("hasNext")] bool HasNext
)
{
    /// <summary>Convenience factory.</summary>
    public static CursorPage<T> Of(IReadOnlyList<T>? items, string? nextCursor, int size)
    {
        var list = items ?? Array.Empty<T>();
        return new CursorPage<T>(list, nextCursor, size, !string.IsNullOrEmpty(nextCursor));
    }
}
