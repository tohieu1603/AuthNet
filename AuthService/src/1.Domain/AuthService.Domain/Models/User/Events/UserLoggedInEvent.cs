using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public sealed record UserLoggedInEvent(
    string UserId,
    string UserName
) : DomainEvent
{
    public override string AggregateId => UserId;
}