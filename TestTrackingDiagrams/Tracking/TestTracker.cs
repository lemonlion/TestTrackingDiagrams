using System.Collections.Concurrent;

namespace TestTrackingDiagrams.Tracking;

public static class TestTracker
{
    private static readonly ConcurrentQueue<TestTrackingLog> Logs = new();

    public static void Log(TestTrackingLog log) => Logs.Enqueue(log);
    public static TestTrackingLog[] TestTrackerLogs => Logs.ToArray();
}