using System.Text.Json;
using AuthService.Application.Events;
using AuthService.Domain.Events;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Messaging;

public class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public bool Enabled { get; set; } = true;
}

public class KafkaIntegrationEventPublisher : IDisposable
{
    private readonly IProducer<string, string>? _producer;
    private readonly DomainEventToIntegrationEventMapper _mapper;
    private readonly ILogger<KafkaIntegrationEventPublisher> _logger;
    private readonly bool _enabled;

    public KafkaIntegrationEventPublisher(
        IConfiguration configuration,
        DomainEventToIntegrationEventMapper mapper,
        ILogger<KafkaIntegrationEventPublisher> logger)
    {
        _mapper = mapper;
        _logger = logger;

        string bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _enabled = configuration.GetValue("Kafka:Enabled", true);

        if (_enabled)
        {
            try
            {
                var config = new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    MaxInFlight = 5,
                    MessageTimeoutMs = 5000
                };
                _producer = new ProducerBuilder<string, string>(config).Build();
                _logger.LogInformation("Kafka producer initialized with bootstrap servers: {Servers}", bootstrapServers);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Kafka producer. Running in degraded mode.");
                _enabled = false;
            }
        }
    }

    public async Task PublishDomainEventAsync(DomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var routed = _mapper.Map(domainEvent);
        if (routed == null) return;

        if (!_enabled || _producer == null)
        {
            _logger.LogDebug("Kafka disabled/unavailable, skipped publishing {EventType}", routed.Event.EventType);
            return;
        }

        try
        {
            string json = JsonSerializer.Serialize(routed.Event, routed.Event.GetType());
            var message = new Message<string, string>
            {
                Key = routed.Key,
                Value = json
            };

            await _producer.ProduceAsync(routed.Topic, message, cancellationToken);
            _logger.LogDebug("Published {EventType} to topic {Topic} [Key={Key}]", routed.Event.EventType, routed.Topic, routed.Key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {EventType} to Kafka topic {Topic}", routed.Event.EventType, routed.Topic);
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
        }
        catch
        {
            // Ignore on dispose
        }
    }
}
