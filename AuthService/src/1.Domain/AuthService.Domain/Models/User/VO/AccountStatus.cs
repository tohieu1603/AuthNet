namespace AuthService.Domain.Models.User.VO;

/// <summary>
/// Value Object representing the security and lifecycle status of a User Account.
/// Implemented as a readonly record struct for native immutability and value-based equality.
/// </summary>
public readonly record struct AccountStatus(
    bool IsEnabled,
    bool IsAccountNonExpired,
    bool IsAccountNonLocked,
    bool IsCredentialsNonExpired,
    DateTimeOffset? LastLogin
)
{
    public static AccountStatus CreateActive() =>
        new(IsEnabled: true, IsAccountNonExpired: true, IsAccountNonLocked: true, IsCredentialsNonExpired: true, LastLogin: null);

    public static AccountStatus CreateDisabled() =>
        new(IsEnabled: false, IsAccountNonExpired: true, IsAccountNonLocked: true, IsCredentialsNonExpired: true, LastLogin: null);

    public static AccountStatus CreateLocked() =>
        new(IsEnabled: true, IsAccountNonExpired: true, IsAccountNonLocked: false, IsCredentialsNonExpired: true, LastLogin: null);

    /// <summary>
    /// Reconstitutes an account status from persistent storage (Database -> Domain Object).
    /// </summary>
    public static AccountStatus Of(
        bool isEnabled,
        bool isAccountNonExpired,
        bool isAccountNonLocked,
        bool isCredentialsNonExpired,
        DateTimeOffset? lastLogin
    ) => new(isEnabled, isAccountNonExpired, isAccountNonLocked, isCredentialsNonExpired, lastLogin);

    public AccountStatus WithLastLogin(DateTimeOffset loginTime) =>
        this with { LastLogin = loginTime };

    public AccountStatus Lock() =>
        this with { IsAccountNonLocked = false };

    public AccountStatus Unlock() =>
        this with { IsAccountNonLocked = true };

    public AccountStatus Disable() =>
        this with { IsEnabled = false };

    public AccountStatus Enable() =>
        this with { IsEnabled = true };

    public bool IsActive => IsEnabled && IsAccountNonExpired && IsAccountNonLocked && IsCredentialsNonExpired;
}