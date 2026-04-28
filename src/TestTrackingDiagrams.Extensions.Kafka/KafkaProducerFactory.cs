using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Default implementation of <see cref="IKafkaProducerFactory{TKey,TValue}"/> that
/// creates producers using <see cref="ProducerBuilder{TKey,TValue}"/>.
/// </summary>
public class KafkaProducerFactory<TKey, TValue> : IKafkaProducerFactory<TKey, TValue>
{
    public IProducer<TKey, TValue> Create(Action<ProducerConfig> configure)
    {
        var config = new ProducerConfig();
        configure(config);
        return new ProducerBuilder<TKey, TValue>(config).Build();
    }
}
