using AuthService.Domain.Events;

namespace AuthService.Domain.Shared;

public abstract class AggregateRoot
{
    private readonly List<DomainEvent> _events = [];

    protected void RegisterEvent(DomainEvent @events)
    {
        if(@events is null) return;
        _events.Add(@events);
    }
    public IReadOnlyList<DomainEvent> PullDomainEvents()
    {
        if(_events.Count == 0) return [];

        var snapshot = _events.ToList().AsReadOnly();
        _events.Clear();
        return snapshot;
    }
    public IReadOnlyList<DomainEvent> PeekDomainEvents() => _events.AsReadOnly();
    protected void ClearDomainEvents() => _events.Clear();
}