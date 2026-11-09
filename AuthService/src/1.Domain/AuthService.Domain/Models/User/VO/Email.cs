using System.Text.RegularExpressions;

namespace AuthService.Domain.Models.User.VO;

public readonly partial record struct Email
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
    public string Value { get; }

public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Email cannot be null or empty.", nameof(value));
        }

        var normalizedValue = value.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(normalizedValue))
        {
            throw new ArgumentException("Email is not in a valid format.", nameof(value));
        }

        Value = normalizedValue;
    }

    public static Email Of(string value) => new(value);

    public string Domain => Value[(Value.IndexOf('@') + 1)..];
    public string LocalPart => Value[..Value.IndexOf('@')];
    public override string ToString() => Value;
}