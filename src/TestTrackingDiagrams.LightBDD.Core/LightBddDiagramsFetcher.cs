namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Provides helper methods to obtain diagram fetcher options configured for LightBDD tests.
/// </summary>
public static class LightBddDiagramsFetcher
{
    public static Func<DefaultDiagramsFetcher.DiagramAsCode[]> GetDiagramsFetcher(DiagramsFetcherOptions? options = null)
    {
        return DefaultDiagramsFetcher.GetDiagramsFetcher(options);
    }
}