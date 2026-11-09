using Refit;

namespace ApiGateway.Clients;

/// <summary>
/// Declarative Refit client for AuthService (equivalent to Java Spring Cloud OpenFeign).
/// Resolves via Cloud Native internal DNS / Service Discovery.
/// </summary>
public interface IAuthServiceClient
{
    [Get("/health")]
    Task<ApiResponse<object>> CheckHealthAsync();

    [Get("/api/v1/users/{userId}")]
    Task<UserSummaryDto> GetUserByIdAsync(
        string userId,
        [Header("Authorization")] string bearerToken,
        CancellationToken cancellationToken = default);
}
