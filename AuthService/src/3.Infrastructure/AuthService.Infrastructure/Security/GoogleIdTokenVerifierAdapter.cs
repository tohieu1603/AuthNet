using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Services;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Security;

public class GoogleIdTokenVerifierAdapter(
    IConfiguration configuration,
    ILogger<GoogleIdTokenVerifierAdapter> logger
) : IGoogleIdTokenVerifierPort
{
    private readonly string? _clientId = configuration["Google:OAuth:ClientId"];

    public async Task<IGoogleIdTokenVerifierPort.GoogleClaims> VerifyAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_clientId))
        {
            throw new GoogleTokenInvalidException("Server not configured for Google login (missing Google:OAuth:ClientId)");
        }

        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new GoogleTokenInvalidException("Empty token");
        }

        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(rawToken, settings);
            if (payload == null)
            {
                throw new GoogleTokenInvalidException("Signature or audience mismatch");
            }

            return new IGoogleIdTokenVerifierPort.GoogleClaims(
                Sub: payload.Subject,
                Email: payload.Email,
                EmailVerified: payload.EmailVerified,
                Name: payload.Name,
                GivenName: payload.GivenName,
                FamilyName: payload.FamilyName,
                Picture: payload.Picture
            );
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Google token invalid: {Message}", ex.Message);
            throw new GoogleTokenInvalidException(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Google token verification error: {Message}", ex.Message);
            throw new GoogleTokenInvalidException(ex.Message);
        }
    }
}
