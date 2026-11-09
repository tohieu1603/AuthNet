using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public sealed record UserCreatedEvent(
    string UserId,
    string UserName,
    string Email
) : DomainEvent
{
    public override string AggregateId => UserId;
}