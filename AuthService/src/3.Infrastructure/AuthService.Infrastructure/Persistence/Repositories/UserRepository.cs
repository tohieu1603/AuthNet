using AuthService.Domain.Events;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Infrastructure.Persistence.Entities;
using AuthService.Infrastructure.Persistence.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class UserRepository(
    AuthDbContext dbContext,
    IDomainEventPublisher eventPublisher
) : IUserRepository
{
    public async Task<User> SaveAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == user.Id.Value, cancellationToken);

        var roleIds = user.Roles.Select(r => r.Value).ToHashSet();
        var roleEntities = roleIds.Count > 0
            ? await dbContext.Roles.Where(r => roleIds.Contains(r.Id)).ToListAsync(cancellationToken)
            : [];

        UserEntity savedEntity;

        if (existing == null)
        {
            var entity = EntityMappers.ToEntity(user, roleEntities);
            await dbContext.Users.AddAsync(entity, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            savedEntity = entity;
        }
        else
        {
            existing.Username = user.Username.Value;
            existing.Email = user.Email.Value;
            existing.Password = user.Password.Value;
            existing.FirstName = user.PersonName.FirstName;
            existing.LastName = user.PersonName.LastName;
            existing.Enabled = user.AccountStatus.IsEnabled;
            existing.AccountNonExpired = user.AccountStatus.IsAccountNonExpired;
            existing.AccountNonLocked = user.AccountStatus.IsAccountNonLocked;
            existing.CredentialsNonExpired = user.AccountStatus.IsCredentialsNonExpired;
            existing.LastLogin = user.AccountStatus.LastLogin;
            existing.TokenVersion = user.TokenVersion;
            existing.GoogleSub = user.GoogleSub?.Value;
            existing.UpdatedAt = user.UpdatedAt;

            existing.Roles.Clear();
            foreach (var r in roleEntities)
            {
                existing.Roles.Add(r);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            savedEntity = existing;
        }

        var events = user.PullDomainEvents();
        await eventPublisher.PublishAllAsync(events, cancellationToken);

        return EntityMappers.ToDomain(savedEntity);
    }

    public async Task<User?> FindByIdAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        return await FindByIdWithRolesAsync(userId, cancellationToken);
    }

    public async Task<User?> FindByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await FindByUsernameWithRolesAsync(username, cancellationToken);
    }

    public async Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<User?> FindByGoogleSubAsync(GoogleSub googleSub, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.GoogleSub == googleSub.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<bool> IsUsernameTakenAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await ExistsByUsernameAsync(username, cancellationToken);
    }

    public async Task<bool> ExistsByUsernameAsync(Username username, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AnyAsync(u => u.Username == username.Value, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AnyAsync(u => u.Email == email.Value, cancellationToken);
    }

    public async Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == user.Id.Value, cancellationToken);

        if (entity != null)
        {
            dbContext.Users.Remove(entity);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<User?> FindByIdWithRolesAsync(UserId userId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<User?> FindByUsernameWithRolesAsync(Username username, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.Users
            .Include(u => u.Roles)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username.Value, cancellationToken);

        return entity != null ? EntityMappers.ToDomain(entity) : null;
    }

    public async Task<IReadOnlyList<User>> FindAfterCursorAsync(
        DateTimeOffset? cursorCreatedAt,
        string? cursorId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IQueryable<UserEntity> query = dbContext.Users
            .Include(u => u.Roles)
            .AsNoTracking();

        if (cursorCreatedAt.HasValue && !string.IsNullOrWhiteSpace(cursorId))
        {
            query = query.Where(u =>
                u.CreatedAt < cursorCreatedAt.Value ||
                (u.CreatedAt == cursorCreatedAt.Value && string.Compare(u.Id, cursorId) < 0));
        }

        var entities = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return entities.Select(EntityMappers.ToDomain).ToList();
    }
}
