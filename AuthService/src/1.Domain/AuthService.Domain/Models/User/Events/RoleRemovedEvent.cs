using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public sealed record RoleRemovedEvent(
    string UserId,
    string RoleId
) : DomainEvent
{
    public override string AggregateId => UserId;
}