using AuthService.Domain.Events;

namespace AuthService.Domain.Models.Role.Events;

public sealed record PermissionRevokedEvent(string RoleId, string PermissionId) : DomainEvent
{
    public override string AggregateId => RoleId;
}