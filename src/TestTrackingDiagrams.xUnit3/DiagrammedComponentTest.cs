using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// Abstract base class for xUnit v3 tests that integrates with the test tracking diagram system to capture test execution context and timing.
/// </summary>
[Collection(DiagrammedTestCollectionName)]
public abstract class DiagrammedComponentTest : IDisposable
{
    public const string DiagrammedTestCollectionName = "Diagrammed Test Collection";

    protected DiagrammedComponentTest()
    {
        // Enable Track.That() assertions to resolve the current test ID.
        Track.TestIdResolver ??= () => TestContext.Current.Test?.UniqueID;
    }

    public void Dispose()
    {
        DiagrammedTestRun.TestContexts.Enqueue(TestContext.Current);
    }
}