namespace TestTrackingDiagrams.Extensions.Npgsql;

/// <summary>
/// Static helper for enabling PostgreSQL test tracking without DI.
/// Because Npgsql uses DiagnosticSource, subscribing once covers ALL connections globally.
/// </summary>
public static class NpgsqlTestTracking
{
    private static NpgsqlDiagnosticTracker? _tracker;
    private static readonly object Lock = new();

    /// <summary>
    /// Ensures a global <see cref="NpgsqlDiagnosticTracker"/> is subscribed to DiagnosticSource.
    /// Call once during test setup. Thread-safe; repeated calls are no-ops.
    /// </summary>
    public static NpgsqlDiagnosticTracker EnsureTracking(NpgsqlTrackingOptions? options = null)
    {
        if (_tracker is not null) return _tracker;

        lock (Lock)
        {
            if (_tracker is not null) return _tracker;

            _tracker = new NpgsqlDiagnosticTracker(options ?? new NpgsqlTrackingOptions());
            _tracker.Subscribe();
            return _tracker;
        }
    }

    /// <summary>
    /// Unsubscribes and disposes the global tracker. Call during test teardown if needed.
    /// </summary>
    public static void Reset()
    {
        lock (Lock)
        {
            _tracker?.Unsubscribe();
            _tracker = null;
        }
    }
}
