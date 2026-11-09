namespace CommonLib.Security;

/// <summary>
/// Canonical HTTP header names used for intra-cluster communication.
/// Produced downstream of the gateway (authenticated by JWT). Never accept them from external callers.
/// </summary>
public static class AuthHeaders
{
    public const string UserId        = "X-User-Id";
    public const string Username      = "X-User-Name";
    public const string Roles         = "X-User-Roles";
    public const string TokenId       = "X-Token-Id";
    public const string TokenVersion  = "X-Token-Version";
    public const string CorrelationId = "X-Correlation-Id";
}
