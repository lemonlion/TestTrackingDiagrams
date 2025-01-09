using LightBDD.Contrib.ReportingEnhancements.Reports;

namespace TestTrackingDiagrams.LightBDD.XUnit;

public static class LightBddDiagramsFetcher
{
    public static Func<DiagramAsCode[]> GetDiagramsFetcher(string plantUmlServerBaseUrl, Func<string, string>? processor = null)
    {
        return () => DiagramsFetcher.GetDiagramsFetcher(plantUmlServerBaseUrl, processor)()
            .Select(x => new DiagramAsCode(Guid.Parse(x.TestRuntimeId), x.ImgSrc, x.CodeBehind)).ToArray();
    }
}