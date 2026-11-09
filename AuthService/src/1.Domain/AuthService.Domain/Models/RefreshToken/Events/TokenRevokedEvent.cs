using AuthService.Domain.Events;

namespace AuthService.Domain.Models.RefreshToken.Events;

public sealed record TokenRevokedEvent(
    string TokenId,
    string UserId,
    string Family,
    string Reason
) : DomainEvent
{
    public override string AggregateId => TokenId;
}