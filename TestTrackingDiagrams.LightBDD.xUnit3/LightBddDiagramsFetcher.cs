using LightBDD.Contrib.ReportingEnhancements.Reports;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

public static class LightBddDiagramsFetcher
{
    public static Func<DiagramAsCode[]> GetDiagramsFetcher(DiagramsFetcherOptions? options = null)
    {
        return () => DefaultDiagramsFetcher.GetDiagramsFetcher(options)()
            .Select(x => new DiagramAsCode(Guid.Parse(x.TestRuntimeId), x.ImgSrc, x.CodeBehind)).ToArray();
    }
}
