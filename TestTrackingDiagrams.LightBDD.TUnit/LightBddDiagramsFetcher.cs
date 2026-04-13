namespace TestTrackingDiagrams.LightBDD.TUnit;

public static class LightBddDiagramsFetcher
{
    public static Func<DefaultDiagramsFetcher.DiagramAsCode[]> GetDiagramsFetcher(DiagramsFetcherOptions? options = null)
    {
        return DefaultDiagramsFetcher.GetDiagramsFetcher(options);
    }
}
