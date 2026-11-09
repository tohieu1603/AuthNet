namespace AuthService.Domain.Models.RefreshToken.VO;

public readonly record struct RevokedReason
{
    public static readonly RevokedReason Normal = new RevokedReason("NORMAL");
    public static readonly RevokedReason ReuseDetected = new("REUSE_DETECTED");
    public static readonly RevokedReason FamilyRevoked = new("FAMILY_REVOKED");
    public static readonly RevokedReason UserInitiated = new("USER_INITIATED");
    public static readonly RevokedReason Expired = new("EXPIRED");
    public string Value { get; }
    public RevokedReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("RevokeReason value cannot be null or empty.", nameof(value));
        }
        
        Value = value.Trim().ToUpperInvariant();
    }
    public static RevokedReason Of(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("RevokeReason value cannot be null or empty.", nameof(value));
        }
        return value.Trim().ToUpperInvariant() switch
        {
            "NORMAL" => Normal,
            "REUSE_DETECTED" => ReuseDetected,
            "FAMILY_REVOKED" => FamilyRevoked,
            "USER_INITIATED" => UserInitiated,
            "EXPIRED" => Expired,
         };
    }
    public bool IsSecurityRelated() => Equals(ReuseDetected) || Equals(FamilyRevoked);
    public bool IsNormal() => Equals(Normal);
    public bool ShouldRevokeFamily() => Equals(FamilyRevoked);
    public override string ToString() => Value;
}