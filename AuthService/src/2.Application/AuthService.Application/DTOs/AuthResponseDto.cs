namespace AuthService.Application.DTOs;

/// <summary>
/// Payload returned after a successful login / register / refresh.
/// </summary>
public sealed record AuthResponseDto(
    string? AccessToken,
    string? RefreshToken,
    string TokenType,
    long ExpiresInSeconds,
    UserDto? User
)
{
    public static AuthResponseDto Bearer(string? accessToken, string? refreshToken, long expiresInSeconds, UserDto? user) =>
        new(accessToken, refreshToken, "Bearer", expiresInSeconds, user);
}
