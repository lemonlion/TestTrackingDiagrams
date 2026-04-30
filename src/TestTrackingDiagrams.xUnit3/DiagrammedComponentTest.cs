using Xunit;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// Abstract base class for xUnit v3 tests that integrates with the test tracking diagram system to capture test execution context and timing.
/// </summary>
[Collection(DiagrammedTestCollectionName)]
public abstract class DiagrammedComponentTest : IDisposable
{
    public const string DiagrammedTestCollectionName = "Diagrammed Test Collection";

    public void Dispose()
    {
        DiagrammedTestRun.TestContexts.Enqueue(TestContext.Current);
    }
}