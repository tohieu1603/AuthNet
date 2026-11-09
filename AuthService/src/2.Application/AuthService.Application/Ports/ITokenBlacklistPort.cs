namespace AuthService.Application.Ports;

/// <summary>
/// Outbound port for access-token revocation lookup.
/// </summary>
public interface ITokenBlacklistPort
{
    Task RevokeAsync(string tokenId, string userId, DateTimeOffset expiresAt, string reason, CancellationToken cancellationToken = default);
    Task<bool> IsRevokedAsync(string tokenId, CancellationToken cancellationToken = default);
}
