using AuthService.Domain.Shared;

namespace AuthService.Application.Common;

/// <summary>
/// Base class for application-layer exceptions.
/// </summary>
public abstract class ApplicationException : Exception
{
    public string Code { get; }

    protected ApplicationException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected ApplicationException(string code, string message, Exception? innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Raised when command/query input fails application-level validation.
/// </summary>
public sealed class ValidationException : ApplicationException
{
    public IReadOnlyDictionary<string, string> FieldErrors { get; }

    public ValidationException(string message)
        : base(ErrorCode.AppBadRequest, message)
    {
        FieldErrors = new Dictionary<string, string>();
    }

    public ValidationException(string message, IDictionary<string, string>? fieldErrors)
        : base(ErrorCode.AppBadRequest, message)
    {
        FieldErrors = fieldErrors != null
            ? new Dictionary<string, string>(fieldErrors)
            : new Dictionary<string, string>();
    }

    public bool HasFieldErrors => FieldErrors.Count > 0;
}
