using Microsoft.Data.Sqlite;

namespace TestTrackingDiagrams.Extensions.Sqlite;

public static class SqliteConnectionExtensions
{
    /// <summary>
    /// Wraps a <see cref="SqliteConnection"/> with a <see cref="TrackingSqliteConnection"/>
    /// for SQL operation tracking in test diagrams.
    /// </summary>
    public static TrackingSqliteConnection WithTestTracking(
        this SqliteConnection connection,
        SqliteTrackingOptions? options = null)
    {
        var opts = options ?? new SqliteTrackingOptions();
        return new TrackingSqliteConnection(connection, opts, opts.HttpContextAccessor);
    }
}
