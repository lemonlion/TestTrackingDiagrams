using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Factory for creating <see cref="IProducer{TKey,TValue}"/> instances.
/// <para>
/// Inject this interface in services that build their own Kafka producers
/// to enable test tracking without changing the producer construction logic.
/// </para>
/// </summary>
public interface IKafkaProducerFactory<TKey, TValue>
{
    /// <summary>
    /// Creates a new <see cref="IProducer{TKey,TValue}"/> using the given configuration action.
    /// The action receives a fresh <see cref="ProducerConfig"/> to configure.
    /// </summary>
    IProducer<TKey, TValue> Create(Action<ProducerConfig> configure);
}
