using System.Text.Json;
using Confluent.Kafka;
using UsersApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace UsersApi.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IOptions<KafkaSettings> options, ILogger<KafkaEventPublisher> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            // Fail fast when the broker is unavailable so a registration is not blocked.
            MessageTimeoutMs = 5000,
            Acks = Acks.All
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task PublishAsync(string topic, string key, object message, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);
            var result = await _producer.ProduceAsync(
                topic, new Message<string, string> { Key = key, Value = json }, ct);
            _logger.LogInformation(
                "Published event to {Topic} partition {Partition} offset {Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (Exception ex)
        {
            // Swallow-and-log: the broker being momentarily unavailable must not fail
            // the caller's use case. At-least-once is not guaranteed (outbox is future work).
            _logger.LogWarning(ex, "Failed to publish event to {Topic}; continuing.", topic);
        }
    }

    public void Dispose()
    {
        try { _producer.Flush(TimeSpan.FromSeconds(5)); } catch { /* best-effort drain */ }
        _producer.Dispose();
    }
}
