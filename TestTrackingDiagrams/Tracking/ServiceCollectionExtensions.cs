using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Tracking;

public static class ServiceCollectionHelper
{
    public static IServiceCollection TrackDependenciesForDiagrams(IServiceCollection services, TestTrackingMessageHandlerOptions options)
    {
        services.AddSingleton(options);
        services.AddHttpContextAccessor();
        services.AddSingleton<IHttpClientFactory, TestTrackingHttpClientFactory>();

        return services;
    }
}