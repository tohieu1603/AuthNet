namespace AuthService.Domain.Models.User.VO;

public readonly record struct Password
{
    private const int MinLength = 8;
    private const int MaxLength = 64;

    public string Value { get; }
    public bool Encoded { get; }

    public Password(string value, bool encoded)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Password cannot be null or empty.", nameof(value));
        }
        if (!encoded)
        {
            ValidatePassword(value);
        }
        Value = value;
        Encoded = encoded;
    }

    public static Password CreateRaw(string value) => new(value, encoded: false);
    public static Password CreateEncoded(string value) => new(value, encoded: true);

    private static void ValidatePassword(string password)
    {
        if (password.Length < MinLength || password.Length > MaxLength)
        {
            throw new ArgumentException($"Password must be between {MinLength} and {MaxLength} characters.", nameof(password));
        }
        if (!password.Any(char.IsLetter))
        {
            throw new ArgumentException("Password must contain at least one letter.", nameof(password));
        }
        if (!password.Any(char.IsDigit))
        {
            throw new ArgumentException("Password must contain at least one digit.", nameof(password));
        }
    }

    public bool NeedsEncoding => !Encoded;
    public override string ToString() => Encoded ? "Encoded Password" : Value;
}