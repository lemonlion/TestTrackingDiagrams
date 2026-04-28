using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.ServiceBus;

/// <summary>
/// Extension methods for registering Service Bus test tracking via dependency injection.
/// </summary>
public static class ServiceBusServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all existing <see cref="ServiceBusClient"/> registrations with
    /// <see cref="TrackingServiceBusClient"/> for test diagram tracking.
    /// <para>
    /// An <see cref="IHttpContextAccessor"/> is resolved from DI (if registered) and wired
    /// into the tracking options automatically.
    /// </para>
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddServiceBusTestTracking(
        this IServiceCollection services,
        Action<ServiceBusTrackingOptions>? configure = null)
    {
        var options = new ServiceBusTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<ServiceBusClient>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingServiceBusClient(inner, options);
        });

        return services;
    }

    /// <summary>
    /// Decorates all existing <see cref="ServiceBusClient"/> registrations with
    /// <see cref="TrackingServiceBusClient"/> using the provided options.
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddServiceBusTestTracking(
        this IServiceCollection services,
        ServiceBusTrackingOptions options)
    {
        services.DecorateAll<ServiceBusClient>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingServiceBusClient(inner, options);
        });

        return services;
    }
}
