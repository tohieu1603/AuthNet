using AuthService.Domain.Events;

namespace AuthService.Domain.Models.User.Events;

public enum AccountStatusTransition
{
    Locked,
    Unlocked,
    Disabled,
    Enabled
}

public sealed record AccountStatusChangedEvent(
    string UserId,
    string UserName,
    AccountStatusTransition Transition
) : DomainEvent
{
    public override string AggregateId => UserId;
    public override string EventType => Transition switch
    {
        AccountStatusTransition.Locked => "AccountLockedEvent",
        AccountStatusTransition.Unlocked => "AccountUnlockedEvent",
        AccountStatusTransition.Disabled => "AccountDisabledEvent",
        AccountStatusTransition.Enabled => "AccountEnabledEvent",
        _ => "AccountStatusChangedEvent"
    };
}
