using System.Collections.Concurrent;
using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit;

public class DiagrammedTestRun
{
    public static ConcurrentQueue<TestContext> TestContexts { get; } = new();
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    protected static void Setup()
    {
        StartRunTime = DateTime.UtcNow;
    }
}