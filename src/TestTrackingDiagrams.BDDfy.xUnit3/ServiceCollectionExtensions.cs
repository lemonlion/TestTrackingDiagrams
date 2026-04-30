using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

/// <summary>
/// Provides extension methods for configuring dependency tracking on <see cref="IServiceCollection"/> for BDDfy tests.
/// </summary>
[Obsolete("Use TestTrackingDiagrams.ServiceCollectionExtensions instead. This wrapper will be removed in a future version.")]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection TrackDependenciesForDiagrams(this IServiceCollection services, BDDfyTestTrackingMessageHandlerOptions options)
    {
        return ServiceCollectionHelper.TrackDependenciesForDiagrams(services, options);
    }
}