namespace AuthService.Domain.Models.User.VO;

public readonly record struct UserId
{
    public string Value { get; }
    public UserId(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException("User ID cannot be null or empty", nameof(value));
        }
        if(!Guid.TryParse(value, out var _))
        {
            throw new ArgumentException("User  ID must be a valid GUID.", nameof(value));
        }
        Value = value;
    }
    public static UserId Of(string value) => new UserId(value);

    public static UserId Generate() => new UserId(Guid.NewGuid().ToString());
}