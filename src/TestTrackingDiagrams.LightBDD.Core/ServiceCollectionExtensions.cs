using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Provides extension methods for configuring dependency tracking on <see cref="IServiceCollection"/> for LightBDD tests.
/// </summary>
[Obsolete("Use TestTrackingDiagrams.ServiceCollectionExtensions instead. This wrapper will be removed in a future version.")]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection TrackDependenciesForDiagrams(
        this IServiceCollection services,
        LightBddTestTrackingMessageHandlerOptions options)
    {
        return ServiceCollectionHelper.TrackDependenciesForDiagrams(services, options);
    }
}