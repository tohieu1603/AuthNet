namespace AuthService.Application.Ports;

public interface IRolePermissionCachePort
{
    Task<IReadOnlySet<string>?> GetAsync(string roleName, CancellationToken cancellationToken = default);
    Task PutAsync(string roleName, IEnumerable<string> permissionNames, CancellationToken cancellationToken = default);
    Task EvictAsync(string roleName, CancellationToken cancellationToken = default);
    Task EvictAllAsync(CancellationToken cancellationToken = default);
}
