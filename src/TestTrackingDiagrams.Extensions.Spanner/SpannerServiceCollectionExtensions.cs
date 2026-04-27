using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.Spanner;

public static class SpannerServiceCollectionExtensions
{
    public static IServiceCollection AddSpannerTestTracking(
        this IServiceCollection services,
        Action<SpannerTrackingOptions>? configure = null)
    {
        var options = new SpannerTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp => new SpannerTracker(options, sp.GetService<IHttpContextAccessor>()));

        return services;
    }
}
