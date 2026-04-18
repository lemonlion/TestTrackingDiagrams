namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// Base class for test classes that participate in diagram generation.
/// Applies <see cref="TestTrackingAttribute"/> automatically and places
/// the test class in the <c>"Diagrammed Test Collection"</c> xUnit collection.
/// <para>
/// In xUnit v2, test identity tracking is handled by <see cref="TestTrackingAttribute"/>
/// via <see cref="AsyncLocal{T}"/>, unlike xUnit v3 which uses <c>TestContext.Current</c>.
/// </para>
/// </summary>
[Xunit.Collection(DiagrammedTestCollectionName)]
[TestTracking]
public abstract class DiagrammedComponentTest : IDisposable
{
    public const string DiagrammedTestCollectionName = "Diagrammed Test Collection";

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }
}
