namespace AuthService.Domain.Models.RefreshToken.VO;

public readonly record struct TokenFamily
{
    public string Value { get; }

    public TokenFamily(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Token family cannot be null or empty.", nameof(value));
        }
        if (!Guid.TryParse(value, out _))
        {
            throw new ArgumentException("Token family must be a valid GUID.", nameof(value));
        }
        Value = value;
    }

    public static TokenFamily Of(string value) => new(value);
    public static TokenFamily Generate() => new(Guid.NewGuid().ToString());

    public bool IsSameAs(TokenFamily other) => Value == other.Value;
    public override string ToString() => Value;
}