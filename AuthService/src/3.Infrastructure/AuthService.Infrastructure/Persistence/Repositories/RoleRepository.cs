using AuthService.Domain.Events;
using AuthService.Domain.Models.Permission.VO;
using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Persistence.Entities;
using AuthService.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class RoleRepository(
    AuthDbContext dbContext,
    IDomainEventPublisher eventPublisher
) : IRoleRepository
{
    public async Task<Role> SaveAsync(Role role, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == role.Id.Value, cancellationToken);

        var permIds = role.Permissions.Select(p => p.Value).ToHashSet();
        var permissionEntities = permIds.Count > 0
            ? await dbContext.Permissions.Where(p => permIds.Contains(p.Id)).ToListAsync(cancellationToken)
            : [];

        RoleEntity savedEntity;

        if (existing == null)
        {
            var entity = EntityMappers.ToEntity(role, permissionEntities);
            await dbContext.Roles.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            savedEntity = entity;
        }
        else
        {
            existing.Name = role.Name.Value;
            existing.Description = role.Description;
            existing.UpdatedAt = role.UpdatedAt;

            existing.Permissions.Clear();
            foreach (var p in permissionEntities)
            {
                existing.Permissions.Add(p);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            savedEntity = existing;
        }

        var events = role.PullDomainEvents();
        await eventPublisher.PublishAllAsync(events, cancellationToken);

        return EntityMappers.ToDomain(savedEntity);
    }

    public async Task<Role?> FindByIdAsync(RoleId roleId, CancellationToken cancellationToken = default)
    {
        return await FindByIdWithPermissionsAsync(roleId, cancellationToken);
    }

    public async Task<Role?> FindByNameAsync(RoleName roleName, CancellationToken cancellationToken = default)
    {
        return await FindByNameWithPermissionsAsync(roleName, cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> FindByIdInAsync(IEnumerable<RoleId> roleIds, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.Select(r => r.Value).ToHashSet();
        var entities = await dbContext.Roles
            .Include(r => r.Permissions)
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }

    public async Task<Role?> FindByIdWithPermissionsAsync(RoleId roleId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Roles
            .Include(r => r.Permissions)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<Role?> FindByNameWithPermissionsAsync(RoleName roleName, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Roles
            .Include(r => r.Permissions)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == roleName.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<bool> ExistsByNameAsync(RoleName roleName, CancellationToken cancellationToken = default)
    {
        return await dbContext.Roles
            .AnyAsync(r => r.Name == roleName.Value, cancellationToken);
    }

    public async Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Roles
            .FirstOrDefaultAsync(r => r.Id == role.Id.Value, cancellationToken);

        if (entity != null)
        {
            dbContext.Roles.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Role>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.Roles
            .Include(r => r.Permissions)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }
}
