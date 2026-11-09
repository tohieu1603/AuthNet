using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User.Events;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Services;
using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User;

/// <summary>
/// User aggregate root.
/// Encapsulates user identity, credentials, roles, and lifecycle states.
/// </summary>
public sealed class User : AggregateRoot, IEquatable<User>
{
    private readonly HashSet<RoleId> _roles = [];

    private User() { }

    public UserId Id { get; private init; }
    public Username Username { get; private set; }
    public Email Email { get; private set; }
    public Password Password { get; private set; }
    public PersonName PersonName { get; private set; }
    public AccountStatus AccountStatus { get; private set; }
    public int TokenVersion { get; private set; }
    public GoogleSub? GoogleSub { get; private set; }
    public DateTimeOffset CreatedAt { get; private init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlySet<RoleId> Roles => _roles;

    /// <summary>
    /// Register a brand-new user with email/password.
    /// </summary>
    public static User Register(
        Username username,
        Email email,
        Password password,
        PersonName personName,
        IPasswordEncoderPort encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = UserId.Generate(),
            Username = username,
            Email = email,
            Password = Password.CreateEncoded(encoder.Encode(password.Value)),
            PersonName = personName,
            AccountStatus = AccountStatus.CreateActive(),
            TokenVersion = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.RegisterEvent(new UserCreatedEvent(user.Id.Value, user.Username.Value, user.Email.Value));
        return user;
    }

    /// <summary>
    /// Rebuilds an aggregate from persistent state (ORM / Repository).
    /// </summary>
    public static User Reconstitute(
        UserId id,
        Username username,
        Email email,
        Password password,
        PersonName personName,
        AccountStatus status,
        IEnumerable<RoleId> roles,
        int tokenVersion,
        GoogleSub? googleSub,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var user = new User
        {
            Id = id,
            Username = username,
            Email = email,
            Password = password,
            PersonName = personName,
            AccountStatus = status,
            TokenVersion = tokenVersion,
            GoogleSub = googleSub,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        if (roles != null)
        {
            user._roles.UnionWith(roles);
        }

        return user;
    }

    /// <summary>
    /// Creates a brand-new account from a verified Google identity.
    /// </summary>
    public static User RegisterFromGoogle(
        Username username,
        Email email,
        PersonName personName,
        GoogleSub googleSub,
        IPasswordEncoderPort encoder)
    {
        ArgumentNullException.ThrowIfNull(encoder);

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Id = UserId.Generate(),
            Username = username,
            Email = email,
            Password = Password.CreateEncoded(encoder.Encode(Guid.NewGuid().ToString())),
            PersonName = personName,
            AccountStatus = AccountStatus.CreateActive(),
            TokenVersion = 1,
            GoogleSub = googleSub,
            CreatedAt = now,
            UpdatedAt = now
        };

        user.RegisterEvent(new UserCreatedEvent(user.Id.Value, username.Value, email.Value));
        user.RegisterEvent(new OAuthProviderLinkedEvent(
            user.Id.Value, "google", googleSub.Value, NewAccount: true));

        return user;
    }

    /// <summary>
    /// Links an existing password account with a Google identity.
    /// </summary>
    public void LinkGoogleAccount(GoogleSub sub)
    {
        if (GoogleSub is not null)
        {
            if (GoogleSub.Equals(sub)) return; // Idempotent
            throw new OAuthAccountAlreadyLinkedException("google");
        }

        GoogleSub = sub;
    }

    public bool IsLinkedWithGoogle => GoogleSub is not null;

    public bool Authenticate(Password rawPassword, IPasswordEncoderPort encoder)
    {
        EnsureAuthenticatable();

        bool matches = encoder.Matches(rawPassword.Value, Password.Value);
        if (matches)
        {
            RecordLogin();
        }
        return matches;
    }

    public void EnsureAuthenticatable()
    {
        var s = AccountStatus;
        if (!s.IsEnabled) throw new AccountNotUsableException(AccountNotUsableException.AccountNotUsableReason.Disabled);
        if (!s.IsAccountNonLocked) throw new AccountNotUsableException(AccountNotUsableException.AccountNotUsableReason.Locked);
        if (!s.IsAccountNonExpired) throw new AccountNotUsableException(AccountNotUsableException.AccountNotUsableReason.Expired);
        if (!s.IsCredentialsNonExpired) throw new AccountNotUsableException(AccountNotUsableException.AccountNotUsableReason.CredentialsExpired);
    }

    private void RecordLogin()
    {
        var now = DateTimeOffset.UtcNow;
        AccountStatus = AccountStatus.WithLastLogin(now);
        UpdatedAt = now;
    }

    public void ChangePassword(Password oldPassword, Password newPassword, IPasswordEncoderPort encoder)
    {
        if (!encoder.Matches(oldPassword.Value, Password.Value))
        {
            throw new InvalidCredentialsException();
        }

        Password = newPassword.NeedsEncoding
            ? Password.CreateEncoded(encoder.Encode(newPassword.Value))
            : newPassword;

        IncrementTokenVersion();
        RegisterEvent(new PasswordChangedEvent(Id.Value, Username.Value));
    }

    public void UpdateEmail(Email email)
    {
        if (Email.Equals(email)) return;
        var oldEmail = Email;
        Email = email;
        UpdatedAt = DateTimeOffset.UtcNow;
        RegisterEvent(new EmailChangedEvent(Id.Value, oldEmail.Value, email.Value));
    }

    public void UpdatePersonName(PersonName name)
    {
        if (PersonName.Equals(name)) return;
        PersonName = name;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Lock() => TransitionStatus(AccountStatus.Lock(), AccountStatusTransition.Locked);
    public void Unlock() => TransitionStatus(AccountStatus.Unlock(), AccountStatusTransition.Unlocked);
    public void Disable() => TransitionStatus(AccountStatus.Disable(), AccountStatusTransition.Disabled);
    public void Enable() => TransitionStatus(AccountStatus.Enable(), AccountStatusTransition.Enabled);

    public void TransitionStatus(AccountStatus next, AccountStatusTransition transition)
    {
        if (AccountStatus.Equals(next)) return;
        AccountStatus = next;
        UpdatedAt = DateTimeOffset.UtcNow;
        RegisterEvent(new AccountStatusChangedEvent(Id.Value, Username.Value, transition));
    }

    public bool IsActive => AccountStatus.IsActive;

    public void AssignRole(RoleId roleId)
    {
        if (_roles.Add(roleId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            RegisterEvent(new RoleAssignedEvent(Id.Value, roleId.Value));
        }
    }

    public void UnassignRole(RoleId roleId)
    {
        if (_roles.Remove(roleId))
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            RegisterEvent(new RoleRemovedEvent(Id.Value, roleId.Value));
        }
    }

    public bool HasRole(RoleId roleId) => _roles.Contains(roleId);

    public void IncrementTokenVersion()
    {
        TokenVersion++;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool Equals(User? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as User);
    public override int GetHashCode() => Id.GetHashCode();
    public static bool operator ==(User? left, User? right) => Equals(left, right);
    public static bool operator !=(User? left, User? right) => !Equals(left, right);
    public override string ToString() => $"User [Id={Id}, Username={Username}, Email={Email}]";
}
