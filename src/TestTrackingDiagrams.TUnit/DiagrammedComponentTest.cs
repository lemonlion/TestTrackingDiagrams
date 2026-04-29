using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

/// <summary>
/// Abstract base class for TUnit tests that integrates with the test tracking diagram system to capture test execution context and timing.
/// </summary>
public abstract class DiagrammedComponentTest
{
    [After(Test)]
    public Task EnqueueTestContext(TestContext context)
    {
        DiagrammedTestRun.TestContexts.Enqueue(context);
        return Task.CompletedTask;
    }
}