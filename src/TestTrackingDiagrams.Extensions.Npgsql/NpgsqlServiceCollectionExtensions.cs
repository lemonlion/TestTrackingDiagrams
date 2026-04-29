using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.Npgsql;

/// <summary>
/// Extension methods for registering PostgreSQL (Npgsql) test tracking via dependency injection.
/// </summary>
public static class NpgsqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers an <see cref="NpgsqlDiagnosticTracker"/> singleton that automatically subscribes
    /// to the Npgsql DiagnosticSource to track all PostgreSQL operations.
    /// <para>
    /// An <see cref="IHttpContextAccessor"/> is resolved from DI (if registered) and wired
    /// into the tracking options automatically.
    /// </para>
    /// </summary>
    public static IServiceCollection AddPostgreSqlTestTracking(
        this IServiceCollection services,
        Action<NpgsqlTrackingOptions>? configure = null)
    {
        var options = new NpgsqlTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            var tracker = new NpgsqlDiagnosticTracker(options, options.HttpContextAccessor);
            tracker.Subscribe();
            return tracker;
        });

        return services;
    }
}
