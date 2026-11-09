using System.Collections.Frozen;
using System.Text.Json;
using AuthService.Domain.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthService.Api.Middleware;

/// <summary>
/// Per-IP rate limiter using an atomic Redis Lua script — direct port of the
/// Java <c>RateLimitFilter</c>. Protects auth endpoints against brute-force
/// and credential-stuffing attacks.
/// <para>Fail-open on Redis failure so a brief outage doesn't lock everyone out.</para>
/// </summary>
public sealed class RateLimitMiddleware(
    RequestDelegate next,
    IConnectionMultiplexer? redis,
    IOptions<RateLimitOptions> options,
    ILogger<RateLimitMiddleware> logger)
{
    private const string KeyPrefix = "rate_limit:";

    // Lua: atomic INCR + EXPIRE, returns {count, ttl}
    private const string LuaScript = """
        local c = redis.call('INCR', KEYS[1])
        if c == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
            return {c, tonumber(ARGV[1])}
        end
        local ttl = redis.call('TTL', KEYS[1])
        return {c, ttl}
        """;

    private static readonly FrozenDictionary<string, string> PathTags = new Dictionary<string, string>
    {
        ["/api/v1/auth/login"]    = "login",
        ["/api/v1/auth/register"] = "register",
        ["/api/v1/auth/refresh"]  = "refresh",
        ["/api/v1/auth/google"]   = "google",
    }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly RateLimitOptions _opts = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!PathTags.TryGetValue(path, out var tag))
        {
            await next(context);
            return;
        }

        var cfg = _opts.EndpointFor(tag);
        if (cfg is null)
        {
            await next(context);
            return;
        }

        var ip = ResolveClientIp(context);

        if (_opts.WhitelistSet.Contains(ip))
        {
            await next(context);
            return;
        }

        var (exceeded, retryAfter) = await ConsumeAsync($"{KeyPrefix}{tag}:{ip}", cfg);

        if (exceeded)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = retryAfter.ToString();
            context.Response.ContentType = "application/json";

            var body = new { code = ErrorCode.AuthRateLimited, message = "Too many requests", retryAfterSeconds = retryAfter };
            await JsonSerializer.SerializeAsync(context.Response.Body, body, cancellationToken: context.RequestAborted);
            return;
        }

        await next(context);
    }

    private string ResolveClientIp(HttpContext ctx)
    {
        var remoteIp = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        bool trustXff = _opts.TrustedProxySet.Count == 0 || _opts.TrustedProxySet.Contains(remoteIp);

        if (trustXff)
        {
            var xff = ctx.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrWhiteSpace(xff))
            {
                ReadOnlySpan<char> span = xff.AsSpan();
                int comma = span.IndexOf(',');
                return (comma > 0 ? span[..comma] : span).Trim().ToString();
            }
        }

        return remoteIp;
    }

    private async Task<(bool Exceeded, long RetryAfter)> ConsumeAsync(string key, EndpointLimit cfg)
    {
        if (redis is null || !redis.IsConnected)
            return (false, cfg.WindowSeconds);

        try
        {
            var db = redis.GetDatabase();
            var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
                LuaScript, [(RedisKey)key], [(RedisValue)cfg.WindowSeconds]);

            if (result is not { Length: >= 2 })
                return (false, cfg.WindowSeconds);

            long count = (long)result[0];
            long ttl   = (long)result[1];
            return (count > cfg.Limit, Math.Max(ttl, 1));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable for rate limit (key={Key}); failing open", key);
            return (false, cfg.WindowSeconds);
        }
    }
}

// ── Configuration ────────────────────────────────────────────────────────────

/// <summary>Rate-limit endpoint configuration — bound from <c>RateLimit</c> section.</summary>
public sealed record EndpointLimit(int Limit, long WindowSeconds)
{
    public EndpointLimit() : this(5, 60) { }
}

/// <summary>
/// Externalised rate-limit configuration.
/// Loaded from <c>RateLimit</c> section so ops can tune without recompiling.
/// </summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public EndpointLimit Login    { get; set; } = new(5, 60);
    public EndpointLimit Register { get; set; } = new(3, 60);
    public EndpointLimit Refresh  { get; set; } = new(20, 60);
    public EndpointLimit Google   { get; set; } = new(10, 60);

    public List<string> Whitelist      { get; set; } = [];
    public List<string> TrustedProxies { get; set; } = [];

    private FrozenSet<string>? _whitelistSet;
    private FrozenSet<string>? _trustedProxySet;

    public FrozenSet<string> WhitelistSet      => _whitelistSet      ??= Whitelist.ToFrozenSet();
    public FrozenSet<string> TrustedProxySet   => _trustedProxySet   ??= TrustedProxies.ToFrozenSet();

    public EndpointLimit? EndpointFor(string tag) => tag switch
    {
        "login"    => Login,
        "register" => Register,
        "refresh"  => Refresh,
        "google"   => Google,
        _          => null
    };
}
