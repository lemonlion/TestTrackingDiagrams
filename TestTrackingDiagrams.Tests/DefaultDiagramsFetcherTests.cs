using System.Reflection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests;

[Collection("DiagramsFetcher")]
public class DefaultDiagramsFetcherTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    public DefaultDiagramsFetcherTests()
    {
        DiagramsField.SetValue(null, null);
        RequestResponseLogger.Clear();
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
        RequestResponseLogger.Clear();
    }

    private static string SeedLog()
    {
        var testId = Guid.NewGuid().ToString();
        RequestResponseLogger.Log(new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Get,
            Content: null,
            Uri: new Uri("http://example.com/api/orders"),
            Headers: [],
            ServiceName: "OrderService",
            CallerName: "WebApp",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false));
        return testId;
    }

    [Fact]
    public void Default_options_produce_png_url()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlRendering = PlantUmlRendering.Server
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("/png/", diagram.ImgSrc);
    }

    [Fact]
    public void Svg_format_produces_svg_url()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Svg,
            PlantUmlRendering = PlantUmlRendering.Server
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("/svg/", diagram.ImgSrc);
        Assert.DoesNotContain("/png/", diagram.ImgSrc);
    }

    [Fact]
    public void Png_format_produces_png_url()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Png,
            PlantUmlRendering = PlantUmlRendering.Server
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("/png/", diagram.ImgSrc);
    }
}
