namespace AuthService.Domain.Shared;

/// <summary>
/// Domain-level error codes mapping directly to canonical <see cref="CommonLib.Error.ErrorCode"/>.
/// </summary>
public static class ErrorCode
{
    // Generic (COMMON)
    public const string ValidationFailed   = CommonLib.Error.ErrorCode.ValidationFailed;
    public const string Unauthorized       = CommonLib.Error.ErrorCode.Unauthorized;
    public const string Forbidden          = CommonLib.Error.ErrorCode.Forbidden;
    public const string NotFound           = CommonLib.Error.ErrorCode.NotFound;
    public const string Conflict           = CommonLib.Error.ErrorCode.Conflict;
    public const string InternalError      = CommonLib.Error.ErrorCode.InternalError;
    public const string ServiceUnavailable = CommonLib.Error.ErrorCode.ServiceUnavailable;

    // App-level fallbacks
    public const string AppBadRequest    = CommonLib.Error.ErrorCode.AppBadRequest;
    public const string AppUnauthorized  = CommonLib.Error.ErrorCode.AppUnauthorized;
    public const string AppForbidden     = CommonLib.Error.ErrorCode.AppForbidden;
    public const string AppConflict      = CommonLib.Error.ErrorCode.AppConflict;
    public const string AppUnprocessable = CommonLib.Error.ErrorCode.AppUnprocessable;
    public const string AppInternal      = CommonLib.Error.ErrorCode.AppInternal;

    // Auth domain (AUTH-1xxx)
    public const string InvalidCredentials     = CommonLib.Error.ErrorCode.InvalidCredentials;
    public const string UserAlreadyExists      = CommonLib.Error.ErrorCode.UserAlreadyExists;
    public const string UserNotFound           = CommonLib.Error.ErrorCode.UserNotFound;
    public const string TokenExpired           = CommonLib.Error.ErrorCode.TokenExpired;
    public const string TokenInvalid           = CommonLib.Error.ErrorCode.TokenInvalid;
    public const string AccountLocked          = CommonLib.Error.ErrorCode.AccountLocked;
    public const string AccountDisabled        = CommonLib.Error.ErrorCode.AccountDisabled;
    public const string RefreshTokenInvalid    = CommonLib.Error.ErrorCode.RefreshTokenInvalid;
    public const string RoleNotFound           = CommonLib.Error.ErrorCode.RoleNotFound;
    public const string PermissionNotFound     = CommonLib.Error.ErrorCode.PermissionNotFound;
    public const string TokenRevoked           = CommonLib.Error.ErrorCode.TokenRevoked;
    public const string TokenReuseDetected     = CommonLib.Error.ErrorCode.TokenReuseDetected;
    public const string TokenOwnershipFail     = CommonLib.Error.ErrorCode.TokenOwnershipFail;
    public const string AuthRateLimited        = CommonLib.Error.ErrorCode.AuthRateLimited;
    public const string AuthBlacklisted        = CommonLib.Error.ErrorCode.AuthBlacklisted;
    public const string AccountExpired         = CommonLib.Error.ErrorCode.AccountExpired;
    public const string CredentialsExpired     = CommonLib.Error.ErrorCode.CredentialsExpired;
    public const string OAuthTokenInvalid      = CommonLib.Error.ErrorCode.OAuthTokenInvalid;
    public const string OAuthEmailUnverified   = CommonLib.Error.ErrorCode.OAuthEmailUnverified;
    public const string OAuthAccountLinked     = CommonLib.Error.ErrorCode.OAuthAccountLinked;
}
