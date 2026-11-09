using System.Text.RegularExpressions;

namespace AuthService.Domain.Models.User.VO;

public readonly partial record struct Username
{
    public const int MinLength = 3;
    public const int MaxLength = 50;

    [GeneratedRegex(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled)]
    public static partial Regex UsernameRegex();

    public string Value { get; }

    public Username(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Username cannot be null or empty.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length < MinLength || trimmed.Length > MaxLength)
        {
            throw new ArgumentException($"Username must be between {MinLength} and {MaxLength} characters.", nameof(value));
        }
        if (!UsernameRegex().IsMatch(trimmed))
        {
            throw new ArgumentException("Username can only contain alphanumeric characters, underscores, and hyphens.", nameof(value));
        }

        Value = trimmed;
    }

    public static Username Of(string value) => new(value);
    public override string ToString() => Value;
}