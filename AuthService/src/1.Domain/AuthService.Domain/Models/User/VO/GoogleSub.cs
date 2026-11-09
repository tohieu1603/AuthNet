namespace AuthService.Domain.Models.User.VO;

public sealed record GoogleSub
{
    public string Value { get; }

    public GoogleSub(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("GoogleSub cannot be null or empty.", nameof(value));
        }
        var trimmed = value.Trim();

        if (trimmed.Length > 255)
        {
            throw new ArgumentException($"GoogleSub too long: {trimmed.Length}", nameof(value));
        }
        Value = trimmed;
    }
    public static GoogleSub Of(string value) => new(value);

    public override string ToString() => Value;
}