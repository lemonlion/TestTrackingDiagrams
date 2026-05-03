using System.Collections.Concurrent;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.MSTest;

/// <summary>
/// Tracks test run lifecycle for MSTest, collecting test contexts and timing information for report generation.
/// </summary>
public class DiagrammedTestRun
{
    public static ConcurrentQueue<MSTestScenarioInfo> TestContexts { get; } = new();
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    protected static void Setup()
    {
        StartRunTime = DateTime.UtcNow;

        // Enable Track.That() assertions to resolve the current test ID.
        Track.TestIdResolver ??= () =>
        {
            var ctx = DiagrammedComponentTest.GetCurrentTestContext();
            return ctx is not null ? $"{ctx.FullyQualifiedTestClassName}.{ctx.TestName}" : null;
        };
    }
}