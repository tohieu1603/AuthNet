namespace AuthService.Application.Events;

/// <summary>
/// Contract for cross-service events published to Kafka.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTimeOffset OccurredOn { get; }
    string AggregateId { get; }
    int SchemaVersion { get; }
}

public static class KafkaTopics
{
    public const string AuthUserEvents = "auth.user.events.v1";
    public const string AuthSessionEvents = "auth.session.events.v1";
}

public sealed record SessionIntegrationEvent(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredOn,
    string AggregateId,
    int SchemaVersion,
    IReadOnlyDictionary<string, object?> Payload
) : IIntegrationEvent;

public sealed record UserLifecycleIntegrationEvent(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredOn,
    string AggregateId,
    int SchemaVersion,
    IReadOnlyDictionary<string, object?> Payload
) : IIntegrationEvent;
