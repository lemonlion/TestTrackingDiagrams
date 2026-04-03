using System.Reflection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.PlantUmlBrowser;

[Collection("DiagramsFetcher")]
public class PlantUmlBrowserDiagramFetcherTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    public PlantUmlBrowserDiagramFetcherTests()
    {
        DiagramsField.SetValue(null, null);
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
    }

    private static string SeedLog(string? content = null)
    {
        var testId = Guid.NewGuid().ToString();
        RequestResponseLogger.Log(new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Get,
            Content: content,
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
    public void PlantUmlBrowser_format_produces_empty_ImgSrc()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.PlantUmlBrowser
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Empty(diagram.ImgSrc);
    }

    [Fact]
    public void PlantUmlBrowser_format_produces_startuml_in_code_behind()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.PlantUmlBrowser
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("@startuml", diagram.CodeBehind);
    }

    [Fact]
    public void PlantUmlBrowser_format_code_behind_contains_autonumber()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.PlantUmlBrowser
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("autonumber", diagram.CodeBehind);
    }

    [Fact]
    public void PlantUmlBrowser_format_code_behind_does_not_contain_sequenceDiagram()
    {
        var testId = SeedLog();
        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.PlantUmlBrowser
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.DoesNotContain("sequenceDiagram", diagram.CodeBehind);
    }

    [Fact]
    public void PlantUmlBrowser_format_passes_through_processor_options()
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
            DiagramFormat = DiagramFormat.PlantUmlBrowser,
            RequestPreFormattingProcessor = c => c.Replace("password123", "REDACTED")
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.Contains("REDACTED", diagram.CodeBehind);
        Assert.DoesNotContain("password123", diagram.CodeBehind);
    }

    [Fact]
    public void PlantUmlBrowser_format_honors_excluded_headers()
    {
        var testId = Guid.NewGuid().ToString();
        RequestResponseLogger.Log(new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Get,
            Content: null,
            Uri: new Uri("http://example.com/api/orders"),
            Headers: [("Authorization", "Bearer secret"), ("Accept", "application/json")],
            ServiceName: "OrderService",
            CallerName: "WebApp",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false));

        var fetcher = DefaultDiagramsFetcher.GetDiagramsFetcher(new DiagramsFetcherOptions
        {
            DiagramFormat = DiagramFormat.PlantUmlBrowser,
            ExcludedHeaders = ["Authorization"]
        });
        var diagrams = fetcher();
        var diagram = diagrams.Single(d => d.TestRuntimeId == testId);

        Assert.DoesNotContain("Bearer secret", diagram.CodeBehind);
        Assert.Contains("Accept", diagram.CodeBehind);
    }
}
