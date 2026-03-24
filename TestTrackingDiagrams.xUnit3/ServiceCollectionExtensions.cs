using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.xUnit3;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection TrackDependenciesForDiagrams(this IServiceCollection services, XUnitTestTrackingMessageHandlerOptions options)
    {
        return ServiceCollectionHelper.TrackDependenciesForDiagrams(services, options);
    }
}