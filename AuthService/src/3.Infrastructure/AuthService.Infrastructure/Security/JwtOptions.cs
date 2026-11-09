namespace AuthService.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = "change-me-change-me-change-me-change-me-please-32chars";
    public long AccessExpirationSeconds { get; set; } = 900;
    public int RefreshExpirationDays { get; set; } = 7;
    public string Issuer { get; set; } = "hieu.com";
}
