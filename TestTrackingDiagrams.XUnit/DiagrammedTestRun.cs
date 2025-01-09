using System.Collections.Concurrent;
using Xunit;

namespace TestTrackingDiagrams.XUnit;
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