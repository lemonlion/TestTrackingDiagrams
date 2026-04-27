using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.PubSub;

/// <summary>
/// Extension methods for registering PubSub test tracking via dependency injection.
/// </summary>
public static class PubSubServiceCollectionExtensions
{
    /// <summary>
    /// Registers a singleton <see cref="PubSubTracker"/> in DI, configured with the provided
    /// options and an <see cref="IHttpContextAccessor"/> resolved from DI (if registered).
    /// <para>
    /// Consumers can then inject <see cref="PubSubTracker"/> when constructing
    /// <see cref="TrackingPublisherClient"/> or <see cref="TrackingSubscriberClient"/> wrappers.
    /// </para>
    /// </summary>
    public static IServiceCollection AddPubSubTestTracking(
        this IServiceCollection services,
        Action<PubSubTrackingOptions>? configure = null)
    {
        var options = new PubSubTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp => new PubSubTracker(options, sp.GetService<IHttpContextAccessor>()));

        return services;
    }
}
