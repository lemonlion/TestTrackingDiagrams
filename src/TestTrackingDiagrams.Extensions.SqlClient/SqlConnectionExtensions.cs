using Microsoft.Data.SqlClient;

namespace TestTrackingDiagrams.Extensions.SqlClient;

/// <summary>
/// Provides extension methods for wrapping a <see cref="SqlConnection"/> with test tracking.
/// </summary>
public static class SqlConnectionExtensions
{
    /// <summary>
    /// Wraps the <see cref="SqlConnection"/> in a <see cref="TrackingSqlConnection"/>
    /// that intercepts all SQL operations for inclusion in test diagrams.
    /// </summary>
    public static TrackingSqlConnection WithTestTracking(
        this SqlConnection connection,
        SqlClientTrackingOptions? options = null)
    {
        var opts = options ?? new SqlClientTrackingOptions();
        return new TrackingSqlConnection(connection, opts, opts.HttpContextAccessor);
    }
}
