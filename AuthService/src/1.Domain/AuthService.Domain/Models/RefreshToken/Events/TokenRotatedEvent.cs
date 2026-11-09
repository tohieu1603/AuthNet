using AuthService.Domain.Events;

namespace AuthService.Domain.Models.RefreshToken.Events;

public sealed record TokenRotatedEvent(
    string UserId,
    string Family,
    string OldTokenId,
    string NewTokenId,
    int NewGeneration
) : DomainEvent
{
    public override string AggregateId => NewTokenId;
}