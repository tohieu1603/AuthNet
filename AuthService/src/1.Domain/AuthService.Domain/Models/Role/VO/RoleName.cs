namespace AuthService.Domain.Models.Role.VO;

public readonly record struct RoleName
{
    private const string RolePrefix = "ROLE_";
    public string Value { get; }

    public RoleName(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Role name cannot be null or empty.", nameof(value));
        }

        var trimmed = value.Trim().ToUpperInvariant();

        Value = trimmed.StartsWith(RolePrefix, StringComparison.Ordinal)
            ? trimmed
            : $"{RolePrefix}{trimmed}";
    }

    public static RoleName Of(string value) => new(value);

    public static RoleName Admin() => new($"{RolePrefix}ADMIN");
    
    public static RoleName User() => new($"{RolePrefix}USER");

    public string SimpleName => Value[RolePrefix.Length..];

    public override string ToString() => Value;
}