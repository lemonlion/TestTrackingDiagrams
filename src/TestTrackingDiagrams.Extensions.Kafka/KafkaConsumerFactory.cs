using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Default implementation of <see cref="IKafkaConsumerFactory{TKey,TValue}"/> that
/// creates consumers using <see cref="ConsumerBuilder{TKey,TValue}"/>.
/// </summary>
public class KafkaConsumerFactory<TKey, TValue> : IKafkaConsumerFactory<TKey, TValue>
{
    public IConsumer<TKey, TValue> Create(Action<ConsumerConfig> configure)
    {
        var config = new ConsumerConfig();
        configure(config);
        return new ConsumerBuilder<TKey, TValue>(config).Build();
    }
}
