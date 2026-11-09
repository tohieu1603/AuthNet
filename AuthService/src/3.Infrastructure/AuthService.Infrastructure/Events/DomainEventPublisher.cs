using AuthService.Domain.Events;
using AuthService.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Events;

public class DomainEventPublisher(
    KafkaIntegrationEventPublisher kafkaPublisher,
    RolePermissionCacheInvalidator cacheInvalidator,
    ILogger<DomainEventPublisher> logger
) : IDomainEventPublisher
{
    public async Task PublishAsync(DomainEvent e, CancellationToken token = default)
    {
        if (e == null) return;

        logger.LogDebug("Publishing {EventType} [EventId={EventId}, AggregateId={AggregateId}]",
            e.EventType, e.EventId, e.AggregateId);

        // Notify cache invalidator
        await cacheInvalidator.HandleDomainEventAsync(e, token);

        // Forward to Kafka integration events
        await kafkaPublisher.PublishDomainEventAsync(e, token);
    }
}
