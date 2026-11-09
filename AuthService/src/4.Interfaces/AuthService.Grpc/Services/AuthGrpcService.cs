using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Ports;
using AuthService.Application.Queries;
using AuthService.Domain.Models.User.Exceptions;
using AuthService.Domain.Models.User.VO;
using AuthService.Domain.Repositories;
using AuthService.Domain.Services;
using AuthService.Grpc.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AuthService.Grpc.Services;

/// <summary>
/// gRPC facade over the application layer — enables sibling microservices to
/// verify JWTs and run authorization checks without HTTP round-trips.
/// All heavy lifting is delegated to the same CQRS handlers used by the REST layer.
/// </summary>
public sealed class AuthGrpcService(
    ITokenProviderPort tokenProvider,
    ITokenBlacklistPort tokenBlacklist,
    IUserRepository userRepository,
    IQueryHandler<CheckRoleQuery, bool> checkRoleHandler,
    IQueryHandler<CheckPermissionQuery, bool> checkPermissionHandler,
    IQueryHandler<GetUserByIdQuery, UserDto> getUserByIdHandler,
    ILogger<AuthGrpcService> logger
) : Protos.AuthService.AuthServiceBase
{
    /// <summary>
    /// Parses + validates an access token. Invalid / expired / revoked tokens
    /// surface as <c>valid=false</c> rather than a gRPC error status.
    /// </summary>
    public override async Task<VerifyTokenResponse> VerifyToken(
        VerifyTokenRequest request, ServerCallContext context)
    {
        var reply = new VerifyTokenResponse();

        try
        {
            var claims = tokenProvider.ParseAccessToken(request.AccessToken);

            if (await tokenBlacklist.IsRevokedAsync(claims.TokenId, context.CancellationToken)
                || !await IsCurrentAndActiveAsync(claims, context.CancellationToken))
            {
                reply.Valid = false;
            }
            else
            {
                reply.Valid        = true;
                reply.UserId       = claims.UserId ?? string.Empty;
                reply.Username     = claims.Username ?? string.Empty;
                reply.TokenVersion = claims.TokenVersion;
                reply.Roles.AddRange(claims.Roles);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug("gRPC verifyToken failed: {Message}", ex.Message);
            reply.Valid = false;
        }

        return reply;
    }

    /// <summary>Delegates to the <see cref="CheckRoleQuery"/> handler.</summary>
    public override async Task<CheckRoleResponse> CheckRole(
        CheckRoleRequest request, ServerCallContext context)
    {
        var hasRole = await checkRoleHandler.HandleAsync(
            new CheckRoleQuery(request.UserId, request.RoleName), context.CancellationToken);

        return new CheckRoleResponse { HasRole = hasRole };
    }

    /// <summary>Delegates to the <see cref="CheckPermissionQuery"/> handler.</summary>
    public override async Task<CheckPermissionResponse> CheckPermission(
        CheckPermissionRequest request, ServerCallContext context)
    {
        var allowed = await checkPermissionHandler.HandleAsync(
            new CheckPermissionQuery(request.UserId, request.PermissionName), context.CancellationToken);

        return new CheckPermissionResponse { Allowed = allowed };
    }

    /// <summary>
    /// Profile lookup by user id. Missing users return <c>found=false</c>
    /// rather than a NOT_FOUND status.
    /// </summary>
    public override async Task<GetUserResponse> GetUser(
        GetUserRequest request, ServerCallContext context)
    {
        var reply = new GetUserResponse();

        try
        {
            var dto = await getUserByIdHandler.HandleAsync(
                new GetUserByIdQuery(request.UserId), context.CancellationToken);

            reply.Found    = true;
            reply.UserId   = dto.Id ?? string.Empty;
            reply.Username = dto.Username ?? string.Empty;
            reply.Email    = dto.Email ?? string.Empty;
            reply.Active   = dto.Enabled && dto.AccountNonLocked
                             && dto.AccountNonExpired && dto.CredentialsNonExpired;
            reply.Roles.AddRange(dto.Roles);
            reply.Permissions.AddRange(dto.Permissions);
        }
        catch (UserNotFoundException)
        {
            reply.Found = false;
        }
        catch (ArgumentException)
        {
            // Malformed (non-UUID) id → "no such user"
            reply.Found = false;
        }

        return reply;
    }

    /// <summary>
    /// Confirms the token's claims still match the live user: tokenVersion
    /// hasn't been bumped and the account is still active.
    /// </summary>
    private async Task<bool> IsCurrentAndActiveAsync(
        ITokenProviderPort.AccessClaims claims, CancellationToken ct)
    {
        try
        {
            var user = await userRepository.FindByIdAsync(UserId.Of(claims.UserId), ct);
            return user is not null
                   && user.IsActive
                   && user.TokenVersion == claims.TokenVersion;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
