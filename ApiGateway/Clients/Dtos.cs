namespace ApiGateway.Clients;

/// <summary>
/// User DTO returned by AuthService
/// </summary>
public record UserSummaryDto(
    string Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    bool Enabled,
    List<string> Roles
);

/// <summary>
/// Token validation result DTO
/// </summary>
public record VerifyTokenResponse(
    bool Valid,
    string? UserId,
    string? Username,
    List<string>? Roles
);
