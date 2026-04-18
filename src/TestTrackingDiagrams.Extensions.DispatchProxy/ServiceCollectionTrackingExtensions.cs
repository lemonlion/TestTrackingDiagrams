using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.DispatchProxy;

public static class ServiceCollectionTrackingExtensions
{
    /// <summary>
    /// Replaces the existing registration for <typeparamref name="TService"/> with a
    /// <see cref="TrackingProxy{T}"/> that wraps the given implementation and records
    /// interactions for diagrams.
    /// </summary>
    public static IServiceCollection ReplaceWithTracked<TService>(
        this IServiceCollection services,
        TService implementation,
        TrackingProxyOptions options) where TService : class
    {
        var proxy = TrackingProxy<TService>.Create(implementation, options);

        // Remove existing registrations
        for (var i = services.Count - 1; i >= 0; i--)
        {
            if (services[i].ServiceType == typeof(TService))
                services.RemoveAt(i);
        }

        services.AddSingleton(proxy);
        return services;
    }

    /// <summary>
    /// Replaces the existing registration for <typeparamref name="TService"/> with a
    /// <see cref="TrackingProxy{T}"/> that wraps the given implementation.
    /// Simplified overload using just service name.
    /// </summary>
    public static IServiceCollection ReplaceWithTracked<TService>(
        this IServiceCollection services,
        TService implementation,
        string serviceName,
        Func<(string Name, string Id)>? testInfoFetcher = null,
        TrackingLogMode logMode = TrackingLogMode.Immediate) where TService : class
    {
        return services.ReplaceWithTracked(implementation, new TrackingProxyOptions
        {
            ServiceName = serviceName,
            CurrentTestInfoFetcher = testInfoFetcher,
            LogMode = logMode
        });
    }
}
