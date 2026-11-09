using AuthService.Domain.Events;

namespace AuthService.Domain.Models.RefreshToken.Events;

public sealed record TokenCreatedEvent(
    string TokenId,
    string UserId,
    string Family,
    int Generation,
    DateTimeOffset ExpiresAt
) : DomainEvent
{
    public override string AggregateId => TokenId;
}