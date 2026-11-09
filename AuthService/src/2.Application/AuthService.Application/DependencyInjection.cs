using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Handlers;
using AuthService.Application.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Command Handlers
        services.AddScoped<ICommandHandler<RegisterUserCommand, AuthResponseDto>, RegisterUserHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, AuthResponseDto>, LoginHandler>();
        services.AddScoped<ICommandHandler<LoginWithGoogleCommand, AuthResponseDto>, LoginWithGoogleHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, AuthResponseDto>, RefreshTokenHandler>();
        services.AddScoped<ICommandHandler<LogoutCommand, bool>, LogoutHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, bool>, ChangePasswordHandler>();
        services.AddScoped<ICommandHandler<UpdateEmailCommand, UserDto>, UpdateEmailHandler>();
        services.AddScoped<ICommandHandler<ChangeAccountStatusCommand, bool>, ChangeAccountStatusHandler>();
        services.AddScoped<ICommandHandler<AssignRoleCommand, bool>, AssignRoleHandler>();
        services.AddScoped<ICommandHandler<UnassignRoleCommand, bool>, UnassignRoleHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetUserByIdQuery, UserDto>, GetUserByIdHandler>();
        services.AddScoped<IQueryHandler<GetUserByUsernameQuery, UserDto>, GetUserByUsernameHandler>();
        services.AddScoped<IQueryHandler<ListUsersQuery, PageDto<UserDto>>, ListUsersHandler>();
        services.AddScoped<IQueryHandler<CheckRoleQuery, bool>, CheckRoleHandler>();
        services.AddScoped<IQueryHandler<CheckPermissionQuery, bool>, CheckPermissionHandler>();

        return services;
    }
}
