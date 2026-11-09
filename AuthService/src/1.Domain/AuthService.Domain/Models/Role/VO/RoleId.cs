namespace AuthService.Domain.Models.Role.VO;

public readonly record struct RoleId
{
    public string Value { get; }
    public RoleId(string value)
    {
        if(string.IsNullOrEmpty(value))
        {
            throw new ArgumentNullException("RoleId cannot be null or empty.");
        }
        var trimmed = value.Trim();
        if(!Guid.TryParse(trimmed, out _))
        {
            throw new ArgumentException("RoleId must be a valid GUID", nameof(value));
        }

        Value = trimmed;
    }
    public static RoleId Generate() => new(Guid.NewGuid().ToString());
    public static RoleId Of(string value) => new(value);
    public override string ToString() => Value;
}