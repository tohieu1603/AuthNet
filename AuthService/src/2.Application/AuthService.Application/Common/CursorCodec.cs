using System.Buffers.Text;
using System.Text;

namespace AuthService.Application.Common;

/// <summary>
/// Opaque cursor codec for keyset pagination.
/// Encodes (createdAt, id) as a URL-safe base64 string.
/// </summary>
public static class CursorCodec
{
    private const string Delim = "|";

    public sealed record Cursor(DateTimeOffset CreatedAt, string Id);

    public static string? Encode(DateTimeOffset? createdAt, string? id)
    {
        if (!createdAt.HasValue || string.IsNullOrWhiteSpace(id)) return null;

        long epochMicros = createdAt.Value.ToUnixTimeMilliseconds() * 1_000L;
        string raw = $"{epochMicros}{Delim}{id}";
        byte[] bytes = Encoding.UTF8.GetBytes(raw);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static Cursor? Decode(string? opaque)
    {
        if (string.IsNullOrWhiteSpace(opaque)) return null;

        try
        {
            string incoming = opaque.Replace('-', '+').Replace('_', '/');
            switch (incoming.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }

            byte[] bytes = Convert.FromBase64String(incoming);
            string raw = Encoding.UTF8.GetString(bytes);

            int split = raw.IndexOf(Delim, StringComparison.Ordinal);
            if (split < 0) throw new FormatException("Missing delimiter");

            long epochMicros = long.Parse(raw[..split]);
            string id = raw[(split + Delim.Length)..];

            long epochMillis = epochMicros / 1_000L;
            var ts = DateTimeOffset.FromUnixTimeMilliseconds(epochMillis);

            return new Cursor(ts, id);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid cursor: {ex.Message}", ex);
        }
    }
}
