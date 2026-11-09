using System.Security.Claims;
using AuthService.Api.DTOs;
using AuthService.Api.Security;
using AuthService.Application.Commands;
using AuthService.Application.Common;
using AuthService.Application.DTOs;
using AuthService.Application.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Controllers;

/// <summary>
/// User-management endpoints.
/// Access control via <c>[Authorize(Roles = "ROLE_ADMIN")]</c> for admin-only operations.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/users")]
[Produces("application/json")]
public sealed class UserController(
    IQueryHandler<GetUserByIdQuery, UserDto> getUserByIdHandler,
    IQueryHandler<ListUsersQuery, PageDto<UserDto>> listUsersHandler,
    IQueryHandler<CheckRoleQuery, bool> checkRoleHandler,
    IQueryHandler<CheckPermissionQuery, bool> checkPermissionHandler,
    ICommandHandler<UpdateEmailCommand, UserDto> updateEmailHandler,
    ICommandHandler<AssignRoleCommand, bool> assignRoleHandler,
    ICommandHandler<UnassignRoleCommand, bool> unassignRoleHandler,
    ICommandHandler<ChangeAccountStatusCommand, bool> changeAccountStatusHandler
) : ControllerBase
{
    /// <summary>Current user's full profile, including effective permissions.</summary>
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(200)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue("userId")!;
        var dto = await getUserByIdHandler.HandleAsync(new GetUserByIdQuery(userId), ct);
        return Ok(dto);
    }

    /// <summary>Update current user's email.</summary>
    [HttpPatch("me/email")]
    [ProducesResponseType<UserDto>(200)]
    public async Task<IActionResult> UpdateMyEmail([FromBody] UpdateEmailRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue("userId")!;
        var dto = await updateEmailHandler.HandleAsync(new UpdateEmailCommand(userId, request.NewEmail), ct);
        return Ok(dto);
    }

    /// <summary>Lookup a user by id (admin + USER_READ permission).</summary>
    [HttpGet("{userId}")]
    [Authorize(Roles = "ROLE_ADMIN")]
    [RequirePermission("USER_READ")]
    [ProducesResponseType<UserDto>(200)]
    public async Task<IActionResult> ById([FromRoute] string userId, CancellationToken ct)
    {
        var dto = await getUserByIdHandler.HandleAsync(new GetUserByIdQuery(userId), ct);
        return Ok(dto);
    }

    /// <summary>List users with cursor pagination (admin + USER_READ permission).</summary>
    [HttpGet]
    [Authorize(Roles = "ROLE_ADMIN")]
    [RequirePermission("USER_READ")]
    [ProducesResponseType<PageDto<UserDto>>(200)]
    public async Task<IActionResult> List(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var page = await listUsersHandler.HandleAsync(new ListUsersQuery(cursor, limit), ct);
        return Ok(page);
    }

    /// <summary>Check whether a user has a role.</summary>
    [HttpGet("{userId}/has-role/{roleName}")]
    [ProducesResponseType<bool>(200)]
    public async Task<IActionResult> HasRole(
        [FromRoute] string userId, [FromRoute] string roleName, CancellationToken ct)
    {
        var result = await checkRoleHandler.HandleAsync(new CheckRoleQuery(userId, roleName), ct);
        return Ok(result);
    }

    /// <summary>Check whether a user has a permission.</summary>
    [HttpGet("{userId}/has-permission/{permissionName}")]
    [ProducesResponseType<bool>(200)]
    public async Task<IActionResult> HasPermission(
        [FromRoute] string userId, [FromRoute] string permissionName, CancellationToken ct)
    {
        var result = await checkPermissionHandler.HandleAsync(
            new CheckPermissionQuery(userId, permissionName), ct);
        return Ok(result);
    }

    /// <summary>Assign a role (admin).</summary>
    [HttpPost("{userId}/roles")]
    [Authorize(Roles = "ROLE_ADMIN")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AssignRole(
        [FromRoute] string userId, [FromBody] RoleAssignmentRequest request, CancellationToken ct)
    {
        await assignRoleHandler.HandleAsync(new AssignRoleCommand(userId, request.RoleName), ct);
        return NoContent();
    }

    /// <summary>Unassign a role (admin).</summary>
    [HttpDelete("{userId}/roles/{roleName}")]
    [Authorize(Roles = "ROLE_ADMIN")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> UnassignRole(
        [FromRoute] string userId, [FromRoute] string roleName, CancellationToken ct)
    {
        await unassignRoleHandler.HandleAsync(new UnassignRoleCommand(userId, roleName), ct);
        return NoContent();
    }

    /// <summary>Transition a user's account status (admin).</summary>
    [HttpPost("{userId}/status/{transition}")]
    [Authorize(Roles = "ROLE_ADMIN")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] string userId, [FromRoute] string transition, CancellationToken ct)
    {
        if (!Enum.TryParse<ChangeAccountStatusCommand.AccountTransition>(transition, ignoreCase: true, out var t))
        {
            return BadRequest(ErrorResponseDto.Of("COMMON-400", $"Invalid transition: {transition}"));
        }

        await changeAccountStatusHandler.HandleAsync(new ChangeAccountStatusCommand(userId, t), ct);
        return NoContent();
    }
}
