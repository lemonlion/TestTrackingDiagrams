using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;

/// <summary>
/// Extension methods for registering Dapper/ADO.NET test tracking via dependency injection.
/// </summary>
public static class DapperServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all existing <see cref="DbConnection"/> registrations with
    /// <see cref="TrackingDbConnection"/> for test diagram tracking.
    /// <para>
    /// An <see cref="IHttpContextAccessor"/> is resolved from DI (if registered) and wired
    /// into the tracking options automatically.
    /// </para>
    /// <para>No-op when no matching registrations exist.</para>
    /// </summary>
    public static IServiceCollection AddDapperTestTracking(
        this IServiceCollection services,
        Action<DapperTrackingOptions>? configure = null)
    {
        var options = new DapperTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<DbConnection>((sp, inner) =>
        {
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingDbConnection(inner, options, options.HttpContextAccessor);
        });

        return services;
    }
}
