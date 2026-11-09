using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public sealed record OAuthProviderLinkedEvent(
    string UserId,
    string Provider,
    string ProviderUserId,
    bool NewAccount
) : DomainEvent
{
    public override string AggregateId => UserId;
}