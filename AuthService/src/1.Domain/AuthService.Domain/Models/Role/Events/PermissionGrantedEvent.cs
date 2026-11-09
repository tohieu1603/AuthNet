using AuthService.Domain.Events;

namespace AuthService.Domain.Models.Role.Events;

public sealed record PermissionGrantedEvent(string RoleId, string PermissionId) : DomainEvent
{
    public override string AggregateId => RoleId;
}