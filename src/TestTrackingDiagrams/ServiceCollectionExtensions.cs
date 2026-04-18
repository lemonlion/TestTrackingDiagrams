using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

/// <summary>
/// Extension methods for configuring HTTP dependency tracking in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="TestTrackingMessageHandler"/> that intercepts outgoing HTTP requests
    /// and records them for diagram generation.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="options">Options controlling which services to track and how to identify them.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection TrackDependenciesForDiagrams(this IServiceCollection services, TestTrackingMessageHandlerOptions options)
    {
        return ServiceCollectionHelper.TrackDependenciesForDiagrams(services, options);
    }
}
