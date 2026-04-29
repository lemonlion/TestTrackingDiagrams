using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.MySqlConnector;

/// <summary>
/// Provides extension methods for configuring MySQL (MySqlConnector) dependency tracking on <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
/// </summary>
public static class MySqlServiceCollectionExtensions
{
    public static IServiceCollection AddMySqlTestTracking(
        this IServiceCollection services,
        Action<MySqlTrackingOptions>? configure = null)
    {
        var options = new MySqlTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            var tracker = new MySqlDiagnosticTracker(options, options.HttpContextAccessor);
            tracker.Subscribe();
            return tracker;
        });

        return services;
    }
}