using AuthService.Application.Ports;
using AuthService.Domain.Events;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;
using AuthService.Infrastructure.Cache;
using AuthService.Infrastructure.Events;
using AuthService.Infrastructure.Messaging;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace AuthService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DB_URL"]
            ?? "Host=localhost;Port=5433;Database=authdb;Username=authuser;Password=authpass";

        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Redis
        string redisHost = configuration["Redis:Host"] ?? configuration["REDIS_HOST"] ?? "localhost";
        string redisPort = configuration["Redis:Port"] ?? configuration["REDIS_PORT"] ?? "6379";
        string redisPassword = configuration["Redis:Password"] ?? configuration["REDIS_PASSWORD"] ?? "";

        var redisOptions = new ConfigurationOptions
        {
            EndPoints = { $"{redisHost}:{redisPort}" },
            Password = string.IsNullOrWhiteSpace(redisPassword) ? null : redisPassword,
            AbortOnConnectFail = false,
            ConnectTimeout = 2000
        };

        try
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        }
        catch
        {
            // Fail-open: Register fallback
            services.AddSingleton<IConnectionMultiplexer>(sp => null!);
        }

        // Options
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // Security & Ports
        services.AddSingleton<RefreshTokenHasher>();
        services.AddSingleton<IPasswordEncoderPort, BCryptPasswordEncoderAdapter>();
        services.AddSingleton<ITokenProviderPort, JwtTokenProvider>();
        services.AddScoped<ITokenBlacklistPort, TokenBlacklistService>();
        services.AddScoped<TokenBlacklistService>();
        services.AddScoped<IGoogleIdTokenVerifierPort, GoogleIdTokenVerifierAdapter>();

        // Cache
        services.AddScoped<IRolePermissionCachePort, RedisRolePermissionCacheAdapter>();
        services.AddScoped<RedisRolePermissionCacheAdapter>();

        // Domain Services
        services.AddSingleton<TokenDomainService>();
        services.AddSingleton<AuthenticationDomainService>();

        // Repositories
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Messaging & Events
        services.AddSingleton<DomainEventToIntegrationEventMapper>();
        services.AddSingleton<KafkaIntegrationEventPublisher>();
        services.AddScoped<RolePermissionCacheInvalidator>();
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        return services;
    }
}
