namespace AuthService.Domain.Services;

/// <summary>
/// Outbound port for Google ID token verification.
/// </summary>
public interface IGoogleIdTokenVerifierPort
{
    Task<GoogleClaims> VerifyAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subset of Google's standard ID-token claims we rely on.
    /// </summary>
    public sealed record GoogleClaims(
        string Sub,
        string Email,
        bool EmailVerified,
        string? Name,
        string? GivenName,
        string? FamilyName,
        string? Picture
    );
}
