using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Factory for creating <see cref="IConsumer{TKey,TValue}"/> instances.
/// <para>
/// Inject this interface in services that build their own Kafka consumers
/// (e.g. <c>BackgroundService</c> classes) to enable test tracking without
/// changing the consumer construction logic.
/// </para>
/// </summary>
public interface IKafkaConsumerFactory<TKey, TValue>
{
    /// <summary>
    /// Creates a new <see cref="IConsumer{TKey,TValue}"/> using the given configuration action.
    /// The action receives a fresh <see cref="ConsumerConfig"/> to configure.
    /// </summary>
    IConsumer<TKey, TValue> Create(Action<ConsumerConfig> configure);
}
