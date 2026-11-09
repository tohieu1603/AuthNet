using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.Role.VO;

namespace AuthService.Domain.Repositories;

/// <summary>
/// Domain Repository Interface for Role Aggregate
/// </summary>
public interface IRoleRepository
{
    Task<Role> SaveAsync(Role role, CancellationToken cancellationToken = default);
    Task<Role?> FindByIdAsync(RoleId roleId, CancellationToken cancellationToken = default);
    Task<Role?> FindByNameAsync(RoleName roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> FindByIdInAsync(IEnumerable<RoleId> roleIds, CancellationToken cancellationToken = default);
    Task<Role?> FindByIdWithPermissionsAsync(RoleId roleId, CancellationToken cancellationToken = default);
    Task<Role?> FindByNameWithPermissionsAsync(RoleName roleName, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(RoleName roleName, CancellationToken cancellationToken = default);
    Task DeleteAsync(Role role, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> FindAllAsync(CancellationToken cancellationToken = default);
}
