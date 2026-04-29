using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.SqlClient;

/// <summary>
/// Provides extension methods for configuring SQL Server (SqlClient) dependency tracking on <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
/// </summary>
public static class SqlClientServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerTestTracking(
        this IServiceCollection services,
        Action<SqlClientTrackingOptions>? configure = null)
    {
        var options = new SqlClientTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            var tracker = new SqlClientDiagnosticTracker(options, options.HttpContextAccessor);
            tracker.Subscribe();
            return tracker;
        });

        return services;
    }
}