using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.Bigtable;

public static class BigtableServiceCollectionExtensions
{
    public static IServiceCollection AddBigtableTestTracking(
        this IServiceCollection services,
        Action<BigtableTrackingOptions>? configure = null)
    {
        var options = new BigtableTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp => new BigtableTracker(options, sp.GetService<IHttpContextAccessor>()));

        return services;
    }
}
