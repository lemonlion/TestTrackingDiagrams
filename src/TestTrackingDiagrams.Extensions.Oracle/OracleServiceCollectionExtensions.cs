using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Oracle.ManagedDataAccess.Client;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.Oracle;

public static class OracleServiceCollectionExtensions
{
    public static IServiceCollection AddOracleTestTracking(
        this IServiceCollection services,
        Action<OracleTrackingOptions>? configure = null)
    {
        var options = new OracleTrackingOptions();
        configure?.Invoke(options);

        services.DecorateAll<DbConnection>((sp, inner) =>
        {
            if (inner is not OracleConnection oracleConn) return inner;
            options.HttpContextAccessor ??= sp.GetService<IHttpContextAccessor>();
            return new TrackingOracleConnection(oracleConn, options, options.HttpContextAccessor);
        });

        return services;
    }
}
