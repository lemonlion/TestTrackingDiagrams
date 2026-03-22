using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

public abstract class DiagrammedComponentTest
{
    [TearDown]
    public void TearDown() => DiagrammedTestRun.TestContexts.Enqueue(TestContext.CurrentContext);
}