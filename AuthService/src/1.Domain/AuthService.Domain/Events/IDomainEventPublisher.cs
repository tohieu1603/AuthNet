namespace AuthService.Domain.Events;

public interface IDomainEventPublisher
{
    Task PublishAsync(DomainEvent e, CancellationToken token = default);
        async Task PublishAllAsync(IEnumerable<DomainEvent> events, CancellationToken token = default)
        {
            foreach (var e in events)
            {
                await PublishAsync(e, token);
            }
        }
}