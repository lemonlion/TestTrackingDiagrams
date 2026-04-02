using System.Collections.Concurrent;

namespace TestTrackingDiagrams.MSTest;

public class DiagrammedTestRun
{
    public static ConcurrentQueue<MSTestScenarioInfo> TestContexts { get; } = new();
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    protected static void Setup()
    {
        StartRunTime = DateTime.UtcNow;
    }
}
