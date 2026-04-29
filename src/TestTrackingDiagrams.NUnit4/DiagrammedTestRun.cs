using System.Collections.Concurrent;
using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

/// <summary>
/// Tracks test run lifecycle for NUnit, collecting test contexts and timing information for report generation.
/// </summary>
public class DiagrammedTestRun
{
    public static ConcurrentQueue<TestContext> TestContexts { get; } = new();
    public static ConcurrentDictionary<string, TimeSpan> TestDurations { get; } = new();
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    protected static void Setup()
    {
        StartRunTime = DateTime.UtcNow;
    }
}