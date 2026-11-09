using AuthService.Domain.Models.Permission;
using AuthService.Domain.Models.Permission.VO;
using AuthService.Domain.Models.RefreshToken;
using AuthService.Domain.Models.RefreshToken.VO;
using AuthService.Domain.Models.Role;
using AuthService.Domain.Models.Role.VO;
using AuthService.Domain.Models.User;
using AuthService.Domain.Models.User.VO;
using AuthService.Infrastructure.Persistence.Entities;

namespace AuthService.Infrastructure.Persistence.Mappers;

public static class EntityMappers
{
    // Permission
    public static Permission ToDomain(PermissionEntity entity)
    {
        return Permission.Reconstitute(
            PermissionId.Of(entity.Id),
            entity.Resource ?? string.Empty,
            entity.Action ?? string.Empty,
            entity.Description,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }

    public static PermissionEntity ToEntity(Permission domain)
    {
        return new PermissionEntity
        {
            Id = domain.Id.Value,
            Name = domain.Name.Value,
            Resource = domain.Name.Resource,
            Action = domain.Name.Action,
            Description = domain.Description,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };
    }

    // Role
    public static Role ToDomain(RoleEntity entity)
    {
        var permissions = entity.Permissions?.Select(p => PermissionId.Of(p.Id));
        return Role.Reconstitute(
            RoleId.Of(entity.Id),
            RoleName.Of(entity.Name),
            entity.Description,
            permissions,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }

    public static RoleEntity ToEntity(Role domain, IEnumerable<PermissionEntity>? permissions = null)
    {
        var entity = new RoleEntity
        {
            Id = domain.Id.Value,
            Name = domain.Name.Value,
            Description = domain.Description,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };

        if (permissions != null)
        {
            foreach (var p in permissions)
            {
                entity.Permissions.Add(p);
            }
        }

        return entity;
    }

    // User
    public static User ToDomain(UserEntity entity)
    {
        var roles = entity.Roles?.Select(r => RoleId.Of(r.Id)) ?? [];
        var status = AccountStatus.Of(
            entity.Enabled,
            entity.AccountNonExpired,
            entity.AccountNonLocked,
            entity.CredentialsNonExpired,
            entity.LastLogin
        );

        return User.Reconstitute(
            UserId.Of(entity.Id),
            Username.Of(entity.Username),
            Email.Of(entity.Email),
            Password.CreateEncoded(entity.Password),
            PersonName.Of(entity.FirstName, entity.LastName),
            status,
            roles,
            entity.TokenVersion,
            entity.GoogleSub != null ? GoogleSub.Of(entity.GoogleSub) : null,
            entity.CreatedAt,
            entity.UpdatedAt
        );
    }

    public static UserEntity ToEntity(User domain, IEnumerable<RoleEntity>? roles = null)
    {
        var entity = new UserEntity
        {
            Id = domain.Id.Value,
            Username = domain.Username.Value,
            Email = domain.Email.Value,
            Password = domain.Password.Value,
            FirstName = domain.PersonName.FirstName,
            LastName = domain.PersonName.LastName,
            Enabled = domain.AccountStatus.IsEnabled,
            AccountNonExpired = domain.AccountStatus.IsAccountNonExpired,
            AccountNonLocked = domain.AccountStatus.IsAccountNonLocked,
            CredentialsNonExpired = domain.AccountStatus.IsCredentialsNonExpired,
            LastLogin = domain.AccountStatus.LastLogin,
            TokenVersion = domain.TokenVersion,
            GoogleSub = domain.GoogleSub?.Value,
            CreatedAt = domain.CreatedAt,
            UpdatedAt = domain.UpdatedAt
        };

        if (roles != null)
        {
            foreach (var r in roles)
            {
                entity.Roles.Add(r);
            }
        }

        return entity;
    }

    // RefreshToken
    public static RefreshToken ToDomain(RefreshTokenEntity entity)
    {
        return RefreshToken.Reconstitute(
            TokenId.Of(entity.Id),
            TokenValue.Of(entity.Token),
            UserId.Of(entity.UserId),
            TokenExpiry.Of(entity.ExpiryDate),
            entity.Revoked,
            entity.CreatedAt,
            entity.RevokedAt,
            TokenFamily.Of(entity.Family),
            GenerationNumber.Of(entity.Generation),
            entity.RevokedReason != null ? RevokedReason.Of(entity.RevokedReason) : null
        );
    }

    public static RefreshTokenEntity ToEntity(RefreshToken domain, string? tokenDigest = null)
    {
        return new RefreshTokenEntity
        {
            Id = domain.Id.Value,
            Token = tokenDigest ?? domain.Value.Value,
            UserId = domain.UserId.Value,
            ExpiryDate = domain.Expiry.Value,
            Revoked = domain.Revoked,
            RevokedAt = domain.RevokedAt,
            Family = domain.Family.Value,
            Generation = domain.Generation.Value,
            RevokedReason = domain.Reason?.Value,
            CreatedAt = domain.CreatedAt
        };
    }
}
