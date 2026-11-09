using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public sealed record EmailChangedEvent(
    string UserId,
    string OldEmail,
    string NewEmail
) : DomainEvent
{
    public override string AggregateId => UserId;
}