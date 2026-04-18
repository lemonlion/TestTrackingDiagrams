using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

public abstract class DiagrammedComponentTest
{
    [After(Test)]
    public Task EnqueueTestContext(TestContext context)
    {
        DiagrammedTestRun.TestContexts.Enqueue(context);
        return Task.CompletedTask;
    }
}
