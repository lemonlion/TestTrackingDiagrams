using Microsoft.Extensions.DependencyInjection;
using Kronikol.Tracking;

namespace Kronikol.ReqNRoll;

/// <summary>
/// Provides extension methods for configuring dependency tracking on <see cref="IServiceCollection"/> for Reqnroll tests.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection TrackDependenciesForDiagrams(this IServiceCollection services, ReqNRollTestTrackingMessageHandlerOptions options)
    {
        return ServiceCollectionHelper.TrackDependenciesForDiagrams(services, options);
    }
}
