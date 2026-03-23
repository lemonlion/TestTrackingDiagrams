using System.Text.Json;
using Microsoft.AspNetCore.Http;
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

    public static IServiceCollection TrackMessagesForDiagrams(
        this IServiceCollection services,
        string callingServiceName,
        JsonSerializerOptions? serializerOptions = null)
    {
        services.AddHttpContextAccessor();
        services.AddSingleton(sp => new MessageTracker(
            sp.GetRequiredService<IHttpContextAccessor>(),
            callingServiceName,
            serializerOptions));

        return services;
    }
}