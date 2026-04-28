using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Tracking decorator for <see cref="IKafkaProducerFactory{TKey,TValue}"/> that wraps
/// every created producer with <see cref="TrackingKafkaProducer{TKey,TValue}"/>.
/// </summary>
public class TrackingKafkaProducerFactory<TKey, TValue> : IKafkaProducerFactory<TKey, TValue>
{
    private readonly IKafkaProducerFactory<TKey, TValue> _inner;
    private readonly KafkaTracker _tracker;
    private readonly KafkaTrackingOptions _options;

    public TrackingKafkaProducerFactory(
        IKafkaProducerFactory<TKey, TValue> inner,
        KafkaTracker tracker,
        KafkaTrackingOptions options)
    {
        _inner = inner;
        _tracker = tracker;
        _options = options;
    }

    public IProducer<TKey, TValue> Create(Action<ProducerConfig> configure)
    {
        var producer = _inner.Create(configure);
        return new TrackingKafkaProducer<TKey, TValue>(producer, _tracker, _options);
    }
}
