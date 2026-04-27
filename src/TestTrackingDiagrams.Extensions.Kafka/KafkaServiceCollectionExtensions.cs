using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Extension methods for registering Kafka test tracking via dependency injection.
/// </summary>
public static class KafkaServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all existing <see cref="IProducer{TKey,TValue}"/> registrations with
    /// <see cref="TrackingKafkaProducer{TKey,TValue}"/> for test diagram tracking.
    /// <para>
    /// The <see cref="KafkaTracker"/> is created internally with the provided options and
    /// an <see cref="IHttpContextAccessor"/> resolved from DI (if registered).
    /// </para>
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddKafkaProducerTestTracking<TKey, TValue>(
        this IServiceCollection services,
        Action<KafkaTrackingOptions>? configure = null)
    {
        var options = new KafkaTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<IProducer<TKey, TValue>>((sp, inner) =>
        {
            var tracker = new KafkaTracker(options, sp.GetService<IHttpContextAccessor>());
            return new TrackingKafkaProducer<TKey, TValue>(inner, tracker, options);
        });

        return services;
    }

    /// <summary>
    /// Decorates all existing <see cref="IConsumer{TKey,TValue}"/> registrations with
    /// <see cref="TrackingKafkaConsumer{TKey,TValue}"/> for test diagram tracking.
    /// <para>
    /// The <see cref="KafkaTracker"/> is created internally with the provided options and
    /// an <see cref="IHttpContextAccessor"/> resolved from DI (if registered).
    /// </para>
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddKafkaConsumerTestTracking<TKey, TValue>(
        this IServiceCollection services,
        Action<KafkaTrackingOptions>? configure = null)
    {
        var options = new KafkaTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<IConsumer<TKey, TValue>>((sp, inner) =>
        {
            var tracker = new KafkaTracker(options, sp.GetService<IHttpContextAccessor>());
            return new TrackingKafkaConsumer<TKey, TValue>(inner, tracker, options);
        });

        return services;
    }
}
