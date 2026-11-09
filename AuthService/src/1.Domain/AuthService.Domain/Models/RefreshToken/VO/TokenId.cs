namespace AuthService.Domain.Models.RefreshToken.VO;

public readonly record struct TokenId
{
    public string Value { get; }

    public TokenId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Token ID cannot be null or empty.", nameof(value));
        }
        if (!Guid.TryParse(value, out _))
        {
            throw new ArgumentException("Token ID must be a valid GUID.", nameof(value));
        }
        Value = value;
    }

    public static TokenId Of(string value) => new(value);
    public static TokenId Generate() => new(Guid.NewGuid().ToString());
    public override string ToString() => Value;
}