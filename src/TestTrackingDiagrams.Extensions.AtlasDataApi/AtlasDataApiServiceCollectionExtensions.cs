using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

public static class AtlasDataApiServiceCollectionExtensions
{
    public static IServiceCollection AddAtlasDataApiTestTracking(
        this IServiceCollection services,
        Action<AtlasDataApiTrackingMessageHandlerOptions>? configure = null)
    {
        var options = new AtlasDataApiTrackingMessageHandlerOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton(sp =>
            new AtlasDataApiTrackingMessageHandler(options, httpContextAccessor: sp.GetService<IHttpContextAccessor>()));

        return services;
    }
}
