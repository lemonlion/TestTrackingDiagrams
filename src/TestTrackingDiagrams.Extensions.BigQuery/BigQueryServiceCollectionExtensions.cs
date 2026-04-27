using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.BigQuery;

public static class BigQueryServiceCollectionExtensions
{
    public static IServiceCollection AddBigQueryTestTracking(
        this IServiceCollection services,
        Action<BigQueryTrackingMessageHandlerOptions>? configure = null)
    {
        var options = new BigQueryTrackingMessageHandlerOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp => new BigQueryTracker(options, sp.GetService<IHttpContextAccessor>()));

        return services;
    }
}
