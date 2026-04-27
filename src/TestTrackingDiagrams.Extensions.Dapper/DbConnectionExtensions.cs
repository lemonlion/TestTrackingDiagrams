using System.Data.Common;

namespace TestTrackingDiagrams;

public static class DbConnectionExtensions
{
    public static TrackingDbConnection WithTestTracking(
        this DbConnection connection,
        DapperTrackingOptions options)
    {
        return new TrackingDbConnection(connection, options, options.HttpContextAccessor);
    }
}
