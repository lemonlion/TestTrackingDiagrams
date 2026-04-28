using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Extension methods for <see cref="IProducer{TKey,TValue}"/> and
/// <see cref="ProducerBuilder{TKey,TValue}"/> that integrate with
/// <see cref="KafkaTrackingInterceptor"/>.
/// </summary>
public static class KafkaProducerExtensions
{
    /// <summary>
    /// Wraps this producer with <see cref="TrackingKafkaProducer{TKey,TValue}"/> if
    /// <see cref="KafkaTrackingInterceptor"/> tracking is enabled for this type combination.
    /// Returns the original producer when tracking is not active (zero overhead in production).
    /// </summary>
    public static IProducer<TKey, TValue> Tracked<TKey, TValue>(
        this IProducer<TKey, TValue> producer)
        => KafkaTrackingInterceptor.WrapProducer(producer);

    /// <summary>
    /// Builds the producer and wraps it with tracking if
    /// <see cref="KafkaTrackingInterceptor"/> is enabled for this type combination.
    /// Use this instead of <c>.Build()</c> for automatic test tracking without
    /// requiring a factory interface.
    /// </summary>
    public static IProducer<TKey, TValue> BuildTracked<TKey, TValue>(
        this ProducerBuilder<TKey, TValue> builder)
    {
        var producer = builder.Build();
        return KafkaTrackingInterceptor.WrapProducer(producer);
    }
}
