using System.Diagnostics;
using System.Text.Json;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Domain.Models.RefreshToken.Exceptions;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Shared;
using Microsoft.Extensions.Logging;

namespace AuthService.Api.Middleware;

/// <summary>
/// Centralised exception-to-HTTP mapping. Catches domain and application
/// exceptions and maps them to structured <see cref="ErrorResponseDto"/> JSON
/// with the correct HTTP status code.
/// </summary>
public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        (int status, string code, string message) = ex switch
        {
            // Domain exceptions → appropriate HTTP codes
            InvalidCredentialsException         => (401, ErrorCode.InvalidCredentials, ex.Message),
            AccountNotUsableException e         => (403, e.Code, e.Message),
            UserNotFoundException               => (404, ErrorCode.UserNotFound, ex.Message),
            UserAlreadyExistsException           => (409, ErrorCode.UserAlreadyExists, ex.Message),
            GoogleTokenInvalidException          => (401, ErrorCode.OAuthTokenInvalid, ex.Message),
            OAuthEmailNotVerifiedException       => (403, ErrorCode.OAuthEmailUnverified, ex.Message),
            OAuthAccountAlreadyLinkedException   => (409, ErrorCode.OAuthAccountLinked, ex.Message),

            // Refresh-token domain exceptions
            TokenRevokedException               => (401, ErrorCode.TokenRevoked, ex.Message),
            TokenExpiredException               => (401, ErrorCode.TokenExpired, ex.Message),
            TokenReuseDetectedException          => (401, ErrorCode.TokenReuseDetected, ex.Message),
            TokenOwnershipException              => (403, ErrorCode.TokenOwnershipFail, ex.Message),

            // Application-layer exceptions
            ValidationException ve              => (400, ErrorCode.ValidationFailed, ve.Message),
            AuthService.Application.Common.ApplicationException ae => (400, ae.Code, ae.Message),

            // Generic DomainException catch-all
            DomainException de                  => (400, de.Code, de.Message),

            // Framework / unknown
            ArgumentException                    => (400, ErrorCode.ValidationFailed, ex.Message),
            UnauthorizedAccessException          => (401, ErrorCode.Unauthorized, "Unauthorized"),
            _                                    => (500, ErrorCode.InternalError, "An unexpected error occurred")
        };

        if (status >= 500)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            logger.LogDebug(ex, "{Code} on {Method} {Path}: {Message}", code, context.Request.Method, context.Request.Path, message);
        }

        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var error   = ErrorResponseDto.Of(code, message, context.Request.Path.Value) with { TraceId = traceId };

        context.Response.StatusCode  = status;
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, error, JsonOpts, context.RequestAborted);
    }
}
