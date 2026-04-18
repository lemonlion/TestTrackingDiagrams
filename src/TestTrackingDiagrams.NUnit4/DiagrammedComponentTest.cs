using System.Diagnostics;
using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

public abstract class DiagrammedComponentTest
{
    private Stopwatch? _stopwatch;

    [SetUp]
    public void TestTrackingSetUp() => _stopwatch = Stopwatch.StartNew();

    [TearDown]
    public void TearDown()
    {
        _stopwatch?.Stop();
        if (_stopwatch is not null)
            DiagrammedTestRun.TestDurations[TestContext.CurrentContext.Test.ID] = _stopwatch.Elapsed;
        DiagrammedTestRun.TestContexts.Enqueue(TestContext.CurrentContext);
    }
}