namespace AuthService.Domain.Models.RefreshToken.VO;

public readonly record struct TokenExpiry
{
    public DateTimeOffset Value { get; }
    
    public TokenExpiry(DateTimeOffset value)
    {
        if (value == default)
        {
            throw new ArgumentNullException(nameof(value), "Token expiry date cannot be default.");
        }

        Value = value;
    }

    public static TokenExpiry Of(DateTimeOffset value) => new(value);

    public static TokenExpiry FromDaysFromNow(int days)
    {
        if (days < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days from now cannot be negative.");
        }
        return new TokenExpiry(DateTimeOffset.UtcNow.AddDays(days));
    }

    public static TokenExpiry FromHoursFromNow(int hours)
    {
        if (hours < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(hours), "Hours from now cannot be negative.");
        }
        return new TokenExpiry(DateTimeOffset.UtcNow.AddHours(hours));
    }

    public bool IsExpired() => Value <= DateTimeOffset.UtcNow;
    public bool IsValid() => !IsExpired();

    public long RemainingSeconds
    {
        get
        {
            if (IsExpired()) return 0;
            var remaining = Value - DateTimeOffset.UtcNow;
            return (long)Math.Max(0, remaining.TotalSeconds);
        }
    }

    public bool WillExpireWithin(long seconds) => RemainingSeconds <= seconds;

    public override string ToString() =>
        $"TokenExpiry: {{ExpiryDate = {Value:o}, IsExpired = {IsExpired()}, RemainingSeconds = {RemainingSeconds}}}";
}