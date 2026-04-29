using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Sqlite;

public static class SqliteServiceCollectionExtensions
{
    /// <summary>
    /// Decorates all existing <see cref="DbConnection"/> registrations with
    /// <see cref="TrackingSqliteConnection"/> for test diagram tracking.
    /// Also decorates <see cref="SqliteConnection"/> registrations if registered directly.
    /// </summary>
    public static IServiceCollection AddSqliteTestTracking(
        this IServiceCollection services,
        Action<SqliteTrackingOptions>? configure = null)
    {
        var options = new SqliteTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<DbConnection>((sp, inner) =>
        {
            if (inner is not SqliteConnection sqliteConn) return inner;
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingSqliteConnection(sqliteConn, options, options.HttpContextAccessor);
        });

        return services;
    }
}
