namespace AuthService.Domain.Models.RefreshToken.VO;

public readonly record struct TokenValue
{
    public string Value { get; }

    public TokenValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Token value cannot be null or empty.", nameof(value));
        }
        Value = value;
    }

    public static TokenValue Of(string value) => new(value);
    public static TokenValue Generate() => new(Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

    public override string ToString() =>
        Value.Length > 8 ? $"{Value[..8]}..." : Value;
}