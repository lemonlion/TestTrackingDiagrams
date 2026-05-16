using Npgsql;

namespace TestTrackingDiagrams.Extensions.Npgsql;

/// <summary>
/// Provides extension methods for wrapping an <see cref="NpgsqlConnection"/> with test tracking.
/// </summary>
public static class NpgsqlConnectionExtensions
{
    /// <summary>
    /// Wraps the <see cref="NpgsqlConnection"/> in a <see cref="TrackingNpgsqlConnection"/>
    /// that intercepts all SQL operations for inclusion in test diagrams.
    /// </summary>
    public static TrackingNpgsqlConnection WithTestTracking(
        this NpgsqlConnection connection,
        NpgsqlTrackingOptions? options = null)
    {
        var opts = options ?? new NpgsqlTrackingOptions();
        return new TrackingNpgsqlConnection(connection, opts, opts.HttpContextAccessor);
    }
}
