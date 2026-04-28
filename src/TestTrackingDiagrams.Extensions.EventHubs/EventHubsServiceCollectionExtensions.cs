using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.EventHubs;

/// <summary>
/// Extension methods for registering Event Hubs test tracking via dependency injection.
/// </summary>
public static class EventHubsServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all existing <see cref="EventHubProducerClient"/> registrations with
    /// <see cref="TrackingEventHubProducerClient"/> for test diagram tracking.
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddEventHubsProducerTestTracking(
        this IServiceCollection services,
        Action<EventHubsTrackingOptions>? configure = null)
    {
        var options = new EventHubsTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<EventHubProducerClient>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingEventHubProducerClient(inner, options);
        });

        return services;
    }

    /// <summary>
    /// Decorates all existing <see cref="EventHubConsumerClient"/> registrations with
    /// <see cref="TrackingEventHubConsumerClient"/> for test diagram tracking.
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddEventHubsConsumerTestTracking(
        this IServiceCollection services,
        Action<EventHubsTrackingOptions>? configure = null)
    {
        var options = new EventHubsTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<EventHubConsumerClient>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingEventHubConsumerClient(inner, options);
        });

        return services;
    }

    /// <summary>
    /// Decorates all existing <see cref="EventHubProducerClient"/> and <see cref="EventHubConsumerClient"/>
    /// registrations for test diagram tracking.
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddEventHubsTestTracking(
        this IServiceCollection services,
        Action<EventHubsTrackingOptions>? configure = null)
    {
        services.AddEventHubsProducerTestTracking(configure);
        services.AddEventHubsConsumerTestTracking(configure);
        return services;
    }
}
