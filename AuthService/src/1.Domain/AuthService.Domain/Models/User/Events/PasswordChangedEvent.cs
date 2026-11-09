using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public sealed record PasswordChangedEvent(
    string UserId,
    string UserName
) : DomainEvent
{
    public override string AggregateId => UserId;
}