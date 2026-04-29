namespace TestTrackingDiagrams.Extensions.MySqlConnector;

public static class MySqlTestTracking
{
    private static MySqlDiagnosticTracker? _tracker;
    private static readonly object Lock = new();

    public static MySqlDiagnosticTracker EnsureTracking(MySqlTrackingOptions? options = null)
    {
        if (_tracker is not null) return _tracker;

        lock (Lock)
        {
            if (_tracker is not null) return _tracker;

            _tracker = new MySqlDiagnosticTracker(options ?? new MySqlTrackingOptions());
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
