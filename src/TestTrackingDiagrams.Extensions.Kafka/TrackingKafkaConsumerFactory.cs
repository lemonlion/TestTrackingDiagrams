using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Tracking decorator for <see cref="IKafkaConsumerFactory{TKey,TValue}"/> that wraps
/// every created consumer with <see cref="TrackingKafkaConsumer{TKey,TValue}"/>.
/// </summary>
public class TrackingKafkaConsumerFactory<TKey, TValue> : IKafkaConsumerFactory<TKey, TValue>
{
    private readonly IKafkaConsumerFactory<TKey, TValue> _inner;
    private readonly KafkaTracker _tracker;
    private readonly KafkaTrackingOptions _options;

    public TrackingKafkaConsumerFactory(
        IKafkaConsumerFactory<TKey, TValue> inner,
        KafkaTracker tracker,
        KafkaTrackingOptions options)
    {
        _inner = inner;
        _tracker = tracker;
        _options = options;
    }

    public IConsumer<TKey, TValue> Create(Action<ConsumerConfig> configure)
    {
        var consumer = _inner.Create(configure);
        return new TrackingKafkaConsumer<TKey, TValue>(consumer, _tracker, _options);
    }
}
