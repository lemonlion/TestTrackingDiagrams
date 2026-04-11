namespace TestTrackingDiagrams.LightBDD.xUnit2;

public static class LightBddDiagramsFetcher
{
    public static Func<DefaultDiagramsFetcher.DiagramAsCode[]> GetDiagramsFetcher(DiagramsFetcherOptions? options = null)
    {
        return DefaultDiagramsFetcher.GetDiagramsFetcher(options);
    }
}