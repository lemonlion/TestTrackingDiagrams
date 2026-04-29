using Oracle.ManagedDataAccess.Client;

namespace TestTrackingDiagrams.Extensions.Oracle;

public static class OracleConnectionExtensions
{
    public static TrackingOracleConnection WithTestTracking(
        this OracleConnection connection,
        OracleTrackingOptions? options = null)
    {
        var opts = options ?? new OracleTrackingOptions();
        return new TrackingOracleConnection(connection, opts, opts.HttpContextAccessor);
    }
}
