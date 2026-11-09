using AuthService.Domain.Shared;

namespace AuthService.Domain.Models.User.Exceptions;

public sealed class AccountNotUsableException : DomainException
{
    public enum AccountNotUsableReason
    {
        Disabled,
        Locked,
        Expired,
        CredentialsExpired
    }

    public AccountNotUsableReason Reason { get; }

    public AccountNotUsableException(AccountNotUsableReason reason)
        : base(reason.GetErrorCode(), reason.GetDefaultMessage())
    {
        Reason = reason;
    }
}

public static class AccountNotUsableExceptionExtensions
{
    public static string GetErrorCode(this AccountNotUsableException.AccountNotUsableReason reason) => reason switch
    {
        AccountNotUsableException.AccountNotUsableReason.Disabled => ErrorCode.AccountDisabled,
        AccountNotUsableException.AccountNotUsableReason.Locked => ErrorCode.AccountLocked,
        AccountNotUsableException.AccountNotUsableReason.Expired => ErrorCode.AccountExpired,
        AccountNotUsableException.AccountNotUsableReason.CredentialsExpired => ErrorCode.CredentialsExpired,
        _ => ErrorCode.AccountDisabled
    };

    public static string GetDefaultMessage(this AccountNotUsableException.AccountNotUsableReason reason) => reason switch
    {
        AccountNotUsableException.AccountNotUsableReason.Disabled => "Account is disabled",
        AccountNotUsableException.AccountNotUsableReason.Locked => "Account is locked",
        AccountNotUsableException.AccountNotUsableReason.Expired => "Account has expired",
        AccountNotUsableException.AccountNotUsableReason.CredentialsExpired => "Credentials have expired",
        _ => "Account is not usable"
    };
}