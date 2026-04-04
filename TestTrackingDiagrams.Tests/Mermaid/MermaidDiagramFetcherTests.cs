using System.Reflection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Mermaid;

[Collection("DiagramsFetcher")]
public class MermaidDiagramFetcherTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    public MermaidDiagramFetcherTests()
    {
        DiagramsField.SetValue(null, null);
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
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
    public void DiagramFormat_defaults_to_PlantUml()
    {
        var options = new DiagramsFetcherOptions();

        Assert.Equal(DiagramFormat.PlantUml, options.DiagramFormat);
    }

    [Fact]
    public void Mermaid_format_produces_diagram_with_sequenceDiagram_in_code_behind()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.Mermaid
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("sequenceDiagram", diagram.CodeBehind);
    }

    [Fact]
    public void Mermaid_format_produces_empty_ImgSrc()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.Mermaid
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Empty(diagram.ImgSrc);
    }

    [Fact]
    public void PlantUml_format_still_produces_png_url()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.PlantUml,
            PlantUmlRendering = PlantUmlRendering.Server
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("/png/", diagram.ImgSrc);
    }

    [Fact]
    public void Mermaid_format_code_behind_contains_autonumber()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.Mermaid
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("autonumber", diagram.CodeBehind);
    }

    [Fact]
    public void Mermaid_format_code_behind_does_not_contain_startuml()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.Mermaid
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.DoesNotContain("@startuml", diagram.CodeBehind);
    }

    [Fact]
    public void Mermaid_format_passes_through_processor_options()
    {
        var testId = Guid.NewGuid().ToString();
        RequestResponseLogger.Log(new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Post,
            Content: """{"secret":"password123"}""",
            Uri: new Uri("http://example.com/api/orders"),
            Headers: [],
            ServiceName: "OrderService",
            CallerName: "WebApp",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false));

        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.Mermaid,
            RequestPreFormattingProcessor = c => c.Replace("password123", "REDACTED")
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("REDACTED", diagram.CodeBehind);
        Assert.DoesNotContain("password123", diagram.CodeBehind);
    }
}
