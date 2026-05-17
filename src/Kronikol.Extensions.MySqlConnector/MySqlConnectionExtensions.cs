using MySqlConnector;

namespace Kronikol.Extensions.MySqlConnector;

/// <summary>
/// Provides extension methods for wrapping a <see cref="MySqlConnection"/> with test tracking.
/// </summary>
public static class MySqlConnectionExtensions
{
    /// <summary>
    /// Wraps the <see cref="MySqlConnection"/> in a <see cref="TrackingMySqlConnection"/>
    /// that intercepts all SQL operations for inclusion in test diagrams.
    /// </summary>
    public static TrackingMySqlConnection WithTestTracking(
        this MySqlConnection connection,
        MySqlTrackingOptions? options = null)
    {
        var opts = options ?? new MySqlTrackingOptions();
        return new TrackingMySqlConnection(connection, opts, opts.HttpContextAccessor);
    }
}
