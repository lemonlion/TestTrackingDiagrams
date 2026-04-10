using System.Reflection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests;

[Collection("DiagramsFetcher")]
public class LocalDiagramRenderingTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    public LocalDiagramRenderingTests()
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
    public void Base64Png_format_produces_data_uri_with_png_mime_type()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Base64Png,
            PlantUmlRendering = PlantUmlRendering.Local,
            LocalDiagramRenderer = (plantUml, format) => [0x89, 0x50, 0x4E, 0x47] // PNG magic bytes
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.StartsWith("data:image/png;base64,", diagram.ImgSrc);
    }

    [Fact]
    public void Base64Svg_format_produces_data_uri_with_svg_mime_type()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Base64Svg,
            PlantUmlRendering = PlantUmlRendering.Local,
            LocalDiagramRenderer = (plantUml, format) => "<svg></svg>"u8.ToArray()
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.StartsWith("data:image/svg+xml;base64,", diagram.ImgSrc);
    }

    [Fact]
    public void Png_format_with_local_renderer_saves_file_and_returns_relative_path()
    {
        var testId = SeedLog();
        var imagesDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "images");
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Png,
            PlantUmlRendering = PlantUmlRendering.Local,
            LocalDiagramRenderer = (plantUml, format) => [0x89, 0x50, 0x4E, 0x47],
            LocalDiagramImageDirectory = imagesDir
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.StartsWith("images/", diagram.ImgSrc);
        Assert.EndsWith(".png", diagram.ImgSrc);

        var fullPath = Path.Combine(Path.GetDirectoryName(imagesDir)!, diagram.ImgSrc);
        Assert.True(File.Exists(fullPath));

        // Clean up
        Directory.Delete(Path.GetDirectoryName(imagesDir)!, true);
    }

    [Fact]
    public void Svg_format_with_local_renderer_saves_file_and_returns_relative_path()
    {
        var testId = SeedLog();
        var imagesDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "images");
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Svg,
            PlantUmlRendering = PlantUmlRendering.Local,
            LocalDiagramRenderer = (plantUml, format) => "<svg></svg>"u8.ToArray(),
            LocalDiagramImageDirectory = imagesDir
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.StartsWith("images/", diagram.ImgSrc);
        Assert.EndsWith(".svg", diagram.ImgSrc);

        var fullPath = Path.Combine(Path.GetDirectoryName(imagesDir)!, diagram.ImgSrc);
        Assert.True(File.Exists(fullPath));

        // Clean up
        Directory.Delete(Path.GetDirectoryName(imagesDir)!, true);
    }

    [Fact]
    public void Local_renderer_receives_plain_text_plantuml()
    {
        var testId = SeedLog();
        string? capturedPlantUml = null;
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Base64Png,
            PlantUmlRendering = PlantUmlRendering.Local,
            LocalDiagramRenderer = (plantUml, format) =>
            {
                capturedPlantUml = plantUml;
                return [0x89, 0x50, 0x4E, 0x47];
            }
        });
        fetcher();

        Assert.NotNull(capturedPlantUml);
        Assert.Contains("@startuml", capturedPlantUml);
        Assert.Contains("@enduml", capturedPlantUml);
    }

    [Fact]
    public void Base64Png_without_local_renderer_throws()
    {
        SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Base64Png,
            PlantUmlRendering = PlantUmlRendering.Local
        });

        Assert.Throws<InvalidOperationException>(() => fetcher());
    }

    [Fact]
    public void Base64Svg_without_local_renderer_throws()
    {
        SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Base64Svg,
            PlantUmlRendering = PlantUmlRendering.Local
        });

        Assert.Throws<InvalidOperationException>(() => fetcher());
    }

    [Fact]
    public void Png_with_local_renderer_but_no_image_directory_throws()
    {
        SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlImageFormat = PlantUmlImageFormat.Png,
            PlantUmlRendering = PlantUmlRendering.Local,
            LocalDiagramRenderer = (plantUml, format) => [0x89, 0x50, 0x4E, 0x47]
        });

        Assert.Throws<InvalidOperationException>(() => fetcher());
    }

    [Fact]
    public void Local_rendering_without_delegate_throws()
    {
        SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            PlantUmlRendering = PlantUmlRendering.Local
        });

        var ex = Assert.Throws<InvalidOperationException>(() => fetcher());
        Assert.Contains("PlantUmlRendering.Local requires a LocalDiagramRenderer", ex.Message);
    }
}
