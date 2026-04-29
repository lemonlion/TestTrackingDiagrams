using System.Collections.Concurrent;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;
/// <summary>
/// Tracks test run lifecycle for xUnit v3, collecting test contexts and timing information for report generation.
/// </summary>
public class DiagrammedTestRun
{
    public static ConcurrentQueue<ITestContext> TestContexts { get; } = new();
    protected static DateTime StartRunTime { get; private set; }
    protected static DateTime EndRunTime { get; set; }

    public DiagrammedTestRun()
    {
        StartRunTime = DateTime.UtcNow;
    }
}