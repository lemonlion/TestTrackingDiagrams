using Confluent.Kafka;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Extension methods for <see cref="IConsumer{TKey,TValue}"/> and
/// <see cref="ConsumerBuilder{TKey,TValue}"/> that integrate with
/// <see cref="KafkaTrackingInterceptor"/>.
/// </summary>
public static class KafkaConsumerExtensions
{
    /// <summary>
    /// Wraps this consumer with <see cref="TrackingKafkaConsumer{TKey,TValue}"/> if
    /// <see cref="KafkaTrackingInterceptor"/> tracking is enabled for this type combination.
    /// Returns the original consumer when tracking is not active (zero overhead in production).
    /// </summary>
    public static IConsumer<TKey, TValue> Tracked<TKey, TValue>(
        this IConsumer<TKey, TValue> consumer)
        => KafkaTrackingInterceptor.WrapConsumer(consumer);

    /// <summary>
    /// Builds the consumer and wraps it with tracking if
    /// <see cref="KafkaTrackingInterceptor"/> is enabled for this type combination.
    /// Use this instead of <c>.Build()</c> for automatic test tracking without
    /// requiring a factory interface.
    /// </summary>
    public static IConsumer<TKey, TValue> BuildTracked<TKey, TValue>(
        this ConsumerBuilder<TKey, TValue> builder)
    {
        var consumer = builder.Build();
        return KafkaTrackingInterceptor.WrapConsumer(consumer);
    }
}
