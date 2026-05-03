using System.Collections.Concurrent;
using TestTrackingDiagrams.Tracking;
using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

/// <summary>
/// Tracks test run lifecycle for TUnit, collecting test contexts and timing information for report generation.
/// </summary>
public class DiagrammedTestRun
{
    public static ConcurrentQueue<TestContext> TestContexts { get; } = new();
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    protected static void Setup()
    {
        StartRunTime = DateTime.UtcNow;

        // Enable Track.That() assertions to resolve the current test ID.
        Track.TestIdResolver ??= () => TestContext.Current?.Id;
    }
}