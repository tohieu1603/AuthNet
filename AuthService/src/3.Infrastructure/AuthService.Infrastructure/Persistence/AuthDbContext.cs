using AuthService.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();
    public DbSet<TokenRevocationEntity> TokenRevocations => Set<TokenRevocationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.GoogleSub)
                .IsUnique()
                .HasFilter("google_sub IS NOT NULL");

            entity.HasMany(e => e.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "user_roles",
                    j => j.HasOne<RoleEntity>().WithMany().HasForeignKey("role_id").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<UserEntity>().WithMany().HasForeignKey("user_id").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("user_id", "role_id");
                        j.ToTable("user_roles");
                    });
        });

        // RoleEntity
        modelBuilder.Entity<RoleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasMany(e => e.Permissions)
                .WithMany(p => p.Roles)
                .UsingEntity<Dictionary<string, object>>(
                    "role_permissions",
                    j => j.HasOne<PermissionEntity>().WithMany().HasForeignKey("permission_id").OnDelete(DeleteBehavior.Cascade),
                    j => j.HasOne<RoleEntity>().WithMany().HasForeignKey("role_id").OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("role_id", "permission_id");
                        j.ToTable("role_permissions");
                    });
        });

        // PermissionEntity
        modelBuilder.Entity<PermissionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // RefreshTokenEntity
        modelBuilder.Entity<RefreshTokenEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Family);
            entity.HasIndex(e => new { e.Revoked, e.ExpiryDate });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TokenRevocationEntity
        modelBuilder.Entity<TokenRevocationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
