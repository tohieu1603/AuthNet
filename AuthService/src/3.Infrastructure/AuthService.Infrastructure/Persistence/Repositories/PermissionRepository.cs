using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Permission.VO;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class PermissionRepository(AuthDbContext dbContext) : IPermissionRepository
{
    public async Task<Permission> SaveAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permission.Id.Value, cancellationToken);

        if (existing == null)
        {
            var entity = EntityMappers.ToEntity(permission);
            await dbContext.Permissions.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            return EntityMappers.ToDomain(entity);
        }
        else
        {
            existing.Name = permission.Name.Value;
            existing.Resource = permission.Name.Resource;
            existing.Action = permission.Name.Action;
            existing.Description = permission.Description;
            existing.UpdatedAt = permission.UpdatedAt;

            await dbContext.SaveChangesAsync(cancellationToken);
            return EntityMappers.ToDomain(existing);
        }
    }

    public async Task<Permission?> FindByIdAsync(PermissionId permissionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == permissionId.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<Permission?> FindByNameAsync(PermissionName permissionName, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Name == permissionName.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<IReadOnlyList<Permission>> FindByIdInAsync(IEnumerable<PermissionId> permissionIds, CancellationToken cancellationToken = default)
    {
        var ids = permissionIds.Select(id => id.Value).ToHashSet();
        var entities = await dbContext.Permissions
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Permission>> FindByResourceAsync(string resource, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.Resource == resource)
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Permission>> FindByActionAsync(string action, CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Permissions
            .AsNoTracking()
            .Where(p => p.Action == action)
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task<Permission?> FindByResourceAndActionAsync(string resource, string action, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Permissions
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Resource == resource && p.Action == action, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<bool> ExistsByNameAsync(PermissionName permissionName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Permissions
            .AnyAsync(p => p.Name == permissionName.Value, cancellationToken);
    }

    public async Task DeleteAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Permissions
            .FirstOrDefaultAsync(p => p.Id == permission.Id.Value, cancellationToken);

        if (entity != null)
        {
            dbContext.Permissions.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Permission>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Permissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }
}
