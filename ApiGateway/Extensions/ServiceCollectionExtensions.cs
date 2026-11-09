using ApiGateway.Clients;
using ApiGateway.Security;
using Refit;
using StackExchange.Redis;

namespace ApiGateway.Extensions;

/// <summary>
/// Clean DI registration extensions.
/// Keeps Program.cs slim by encapsulating all service registrations here.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers Redis connection with fail-safe (non-throwing) behavior.
    /// </summary>
    public static IServiceCollection AddGatewayRedis(this IServiceCollection services, IConfiguration configuration)
    {
        var redisHost = configuration["Redis:Host"] ?? "localhost";
        var redisPort = configuration["Redis:Port"] ?? "6379";

        try
        {
            var redis = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { $"{redisHost}:{redisPort}" },
                AbortOnConnectFail = false,
                ConnectTimeout = 2000
            });
            services.AddSingleton<IConnectionMultiplexer>(redis);
        }
        catch
        {
            // Fail-open: Gateway continues without Redis
            services.AddSingleton<IConnectionMultiplexer>(sp => null!);
        }

        services.AddSingleton<ITokenRevocationValidator, RedisTokenRevocationValidator>();

        return services;
    }

    /// <summary>
    /// Registers Refit declarative client for AuthService with Polly resilience.
    /// </summary>
    public static IServiceCollection AddGatewayRefitClients(this IServiceCollection services, IConfiguration configuration)
    {
        var authServiceUrl = configuration["Services:AuthService"] ?? "http://localhost:5000";

        services.AddRefitClient<IAuthServiceClient>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(authServiceUrl);
                c.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Registers CORS policy allowing all origins with credentials.
    /// </summary>
    public static IServiceCollection AddGatewayCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetIsOriginAllowed(_ => true)
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Registers YARP Reverse Proxy with multi-file configuration loading.
    /// Loads routes from Config/routes.*.json files merged with appsettings.json.
    /// </summary>
    public static IServiceCollection AddGatewayYarp(this IServiceCollection services, ConfigurationManager configuration)
    {
        // Load modular route configs from Config/ directory
        var configDir = Path.Combine(AppContext.BaseDirectory, "Config");
        if (Directory.Exists(configDir))
        {
            foreach (var routeFile in Directory.GetFiles(configDir, "routes.*.json"))
            {
                configuration.AddJsonFile(routeFile, optional: true, reloadOnChange: true);
            }
        }

        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }
}
