namespace TestTrackingDiagrams.Extensions.SqlClient;

/// <summary>
/// Provides methods for enabling SQL Server (SqlClient) test tracking on database connections.
/// </summary>
public static class SqlClientTestTracking
{
    private static SqlClientDiagnosticTracker? _tracker;
    private static readonly object Lock = new();

    public static SqlClientDiagnosticTracker EnsureTracking(SqlClientTrackingOptions? options = null)
    {
        if (_tracker is not null) return _tracker;

        lock (Lock)
        {
            if (_tracker is not null) return _tracker;

            _tracker = new SqlClientDiagnosticTracker(options ?? new SqlClientTrackingOptions());
            _tracker.Subscribe();
            return _tracker;
        }
    }

    public static void Reset()
    {
        lock (Lock)
        {
            _tracker?.Unsubscribe();
            _tracker = null;
        }
    }
}