using Xunit;

namespace TestTrackingDiagrams.XUnit;

[Collection(DiagrammedTestCollectionName)]
public abstract class DiagrammedComponentTest : IDisposable
{
    public const string DiagrammedTestCollectionName = "Diagrammed Test Collection";

    public void Dispose()
    {
        DiagrammedTestRun.TestContexts.Enqueue(TestContext.Current);
    }
}