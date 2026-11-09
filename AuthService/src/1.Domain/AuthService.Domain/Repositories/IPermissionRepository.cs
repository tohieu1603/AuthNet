using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Permission.VO;

namespace AuthService.Domain.Repositories;

/// <summary>
/// Domain Repository Interface for Permission Entity
/// </summary>
public interface IPermissionRepository
{
    Task<Permission> SaveAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<Permission?> FindByIdAsync(PermissionId permissionId, CancellationToken cancellationToken = default);
    Task<Permission?> FindByNameAsync(PermissionName permissionName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> FindByIdInAsync(IEnumerable<PermissionId> permissionIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> FindByResourceAsync(string resource, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> FindByActionAsync(string action, CancellationToken cancellationToken = default);
    Task<Permission?> FindByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(PermissionName permissionName, CancellationToken cancellationToken = default);
    Task DeleteAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Permission>> FindAllAsync(CancellationToken cancellationToken = default);
}
