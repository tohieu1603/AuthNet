using AuthService.Application.Common;
using AuthService.Application.DTOs;

namespace AuthService.Application.Queries;

public sealed record GetUserByIdQuery(string UserId) : IQuery<UserDto>;

public sealed record GetUserByUsernameQuery(string Username) : IQuery<UserDto>;

public sealed record ListUsersQuery(string? Cursor, int Limit) : IQuery<PageDto<UserDto>>;

public sealed record CheckRoleQuery(string UserId, string RoleName) : IQuery<bool>;

public sealed record CheckPermissionQuery(string UserId, string PermissionName) : IQuery<bool>;
