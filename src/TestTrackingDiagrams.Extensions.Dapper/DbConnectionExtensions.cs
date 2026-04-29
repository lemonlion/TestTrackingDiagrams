using System.Data.Common;

namespace TestTrackingDiagrams;

/// <summary>
/// Provides extension methods for configuring Dapper client options to enable test tracking.
/// </summary>
public static class DbConnectionExtensions
{
    public static TrackingDbConnection WithTestTracking(
        this DbConnection connection,
        DapperTrackingOptions options)
    {
        return new TrackingDbConnection(connection, options, options.HttpContextAccessor);
    }
}