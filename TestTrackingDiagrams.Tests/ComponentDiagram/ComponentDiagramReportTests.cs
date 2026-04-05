using System.Diagnostics;
using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class ComponentDiagramReportTests : IDisposable
{
    private readonly string _reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

    public void Dispose()
    {
        // Cleanup generated test files
        foreach (var file in Directory.GetFiles(_reportDir, "ComponentDiagram*"))
            File.Delete(file);
    }

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string callerName = "Caller",
        string serviceName = "OrderService",
        string method = "GET",
        Guid? requestResponseId = null)
    {
        return new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: null,
            Uri: new Uri("http://example.com/api"),
            Headers: [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: requestResponseId ?? Guid.NewGuid(),
            TrackingIgnore: false);
    }

    [Fact]
    public void GenerateComponentDiagramReport_CreatesPumlFile()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.True(File.Exists(result.PumlFilePath));
        var content = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("@startuml", content);
        Assert.Contains("@enduml", content);
        Assert.Contains("OrderService", content);
    }

    [Fact]
    public void GenerateComponentDiagramReport_CreatesHtmlFile()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.True(File.Exists(result.HtmlFilePath));
        var content = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<html>", content);
        Assert.Contains("Component Diagram", content);
        Assert.Contains("@startuml", content);
    }

    [Fact]
    public void GenerateComponentDiagramReport_EmptyLogs_StillGeneratesFiles()
    {
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport([], new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.True(File.Exists(result.PumlFilePath));
        Assert.True(File.Exists(result.HtmlFilePath));
    }

    [Fact]
    public void GenerateComponentDiagramReport_CustomFileName_UsesIt()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions { FileName = "MyDiagram" };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        Assert.Contains("MyDiagram.puml", result.PumlFilePath);
        Assert.Contains("MyDiagram.html", result.HtmlFilePath);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PlantUmlContent_IsCorrect()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "WebApp", serviceName: "OrderService", method: "POST"),
            MakeRequest(callerName: "OrderService", serviceName: "PaymentService", method: "POST")
        };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var puml = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("Person(", puml);
        Assert.Contains("WebApp", puml);
        Assert.Contains("System(", puml);
        Assert.Contains("OrderService", puml);
        Assert.Contains("PaymentService", puml);
        Assert.Contains("Rel(", puml);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HtmlContainsImageTag()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<img src=", html);
        Assert.Contains("plantuml.com/plantuml", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HtmlEncodesPlantUmlSource()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var html = File.ReadAllText(result.HtmlFilePath);
        // The URL-based include should render correctly in browser (no angle brackets to escape)
        Assert.Contains("C4_Context.puml", html);
        Assert.DoesNotContain("<C4/", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ServerUrl_ContainsEncodedPlantUml()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlServerBaseUrl = "https://plantuml.com/plantuml", PlantUmlImageFormat = PlantUmlImageFormat.Svg });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml.com/plantuml/svg/", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PngFormat_UsesPngUrl()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlImageFormat = PlantUmlImageFormat.Png });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml.com/plantuml/png/", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_LocalRenderer_UsesRenderedImage()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();
        var fakeImageBytes = "fake-image-data"u8.ToArray();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = options,
                PlantUmlImageFormat = PlantUmlImageFormat.Base64Png,
                PlantUmlRendering = PlantUmlRendering.Local,
                LocalDiagramRenderer = (_, _) => fakeImageBytes
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("data:image/png;base64,", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PumlFile_UsesC4Context()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var puml = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml", puml);
        Assert.DoesNotContain("C4_Component", puml);
    }

    // ── Relationship Flow Integration ──

    private (Activity span, Dictionary<string, InternalFlowSegment> segments, RequestResponseLog[] logs) CreateFlowTestData(
        string callerName = "WebApp", string serviceName = "OrderService")
    {
        var source = new ActivitySource("Tests.CompDiagram." + Guid.NewGuid().ToString("N")[..6]);
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        var span = source.StartActivity("DB Query")!;
        span.SetStartTime(DateTime.UtcNow);
        span.SetEndTime(DateTime.UtcNow.AddMilliseconds(50));

        var reqId = Guid.NewGuid();
        var logs = new[] { MakeRequest(callerName: callerName, serviceName: serviceName, requestResponseId: reqId) };

        var perBoundarySegments = new Dictionary<string, InternalFlowSegment>
        {
            [$"iflow-req-{reqId}"] = new(reqId, RequestResponseType.Request, "test-1",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMilliseconds(50), [span])
        };

        return (span, perBoundarySegments, logs);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ShowRelationshipFlows_RendersRelationshipList()
    {
        var (span, perBoundary, logs) = CreateFlowTestData();
        using var _ = span;
        var options = new ComponentDiagramOptions { ShowRelationshipFlows = true };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = options },
            perBoundarySegments: perBoundary);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("iflow-rel-list", html);
        Assert.Contains("WebApp", html);
        Assert.Contains("OrderService", html);
        Assert.Contains("_iflowShowPopup", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ShowRelationshipFlows_IncludesPopupInfrastructure()
    {
        var (span, perBoundary, logs) = CreateFlowTestData("API", "DB");
        using var _ = span;
        var options = new ComponentDiagramOptions { ShowRelationshipFlows = true };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = options },
            perBoundarySegments: perBoundary);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("__iflowSegments", html);
        Assert.Contains("iflow-popup", html);
        Assert.Contains("iflow-overlay", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ShowRelationshipFlows_PopupDataContainsSummaryTable()
    {
        var (span, perBoundary, logs) = CreateFlowTestData("API", "DB");
        using var _ = span;
        var options = new ComponentDiagramOptions { ShowRelationshipFlows = true };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = options },
            perBoundarySegments: perBoundary);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("iflow-rel-summary-table", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ShowSystemFlameChart_RendersSystemSection()
    {
        using var source = new ActivitySource("Tests.CompDiagram.System");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        using var span = source.StartActivity("Process")!;
        span.SetStartTime(DateTime.UtcNow);
        span.SetEndTime(DateTime.UtcNow.AddMilliseconds(50));

        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions { ShowSystemFlameChart = true };

        var wholeTestSegments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = new(Guid.Empty, RequestResponseType.Request, "test-1",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMilliseconds(50), [span])
        };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = options },
            wholeTestSegments: wholeTestSegments);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("System Flow", html);
        Assert.Contains("iflow-sequential-tests", html);
        Assert.Contains("iflow-toggle", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_NoFlows_DoesNotIncludeFlowSections()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("iflow-rel-list", html);
        Assert.DoesNotContain("System Flow", html);
        Assert.DoesNotContain("__iflowSegments", html);
    }
}
