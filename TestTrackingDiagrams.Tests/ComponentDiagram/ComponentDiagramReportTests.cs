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

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlRendering = PlantUmlRendering.Server });

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
        // The stdlib include uses angle brackets which get HTML-encoded in the data attribute
        Assert.Contains("C4/C4_Context", html);
        Assert.DoesNotContain("raw.githubusercontent.com", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ServerUrl_ContainsEncodedPlantUml()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlServerBaseUrl = "https://plantuml.com/plantuml", PlantUmlImageFormat = PlantUmlImageFormat.Svg, PlantUmlRendering = PlantUmlRendering.Server });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml.com/plantuml/svg/", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PngFormat_UsesPngUrl()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = options, PlantUmlImageFormat = PlantUmlImageFormat.Png, PlantUmlRendering = PlantUmlRendering.Server });

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
        Assert.Contains("!include <C4/C4_Context>", puml);
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
    public void GenerateComponentDiagramReport_ShowRelationshipFlows_DataScriptBeforePopupScript()
    {
        var (span, perBoundary, logs) = CreateFlowTestData("API", "DB");
        using var _ = span;
        var options = new ComponentDiagramOptions { ShowRelationshipFlows = true };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = options },
            perBoundarySegments: perBoundary);

        var html = File.ReadAllText(result.HtmlFilePath);
        var dataIndex = html.IndexOf("window.__iflowSegments =");
        var popupIndex = html.IndexOf("var iflowData = window.__iflowSegments");
        Assert.True(dataIndex > 0, "__iflowSegments assignment not found");
        Assert.True(popupIndex > 0, "iflowData capture not found");
        Assert.True(dataIndex < popupIndex,
            $"Data script (index {dataIndex}) must appear before popup script (index {popupIndex}) " +
            "so the IIFE captures the populated data instead of an empty object");
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
        Assert.DoesNotContain("window.__iflowSegments = {", html);
    }

    [Fact]
    public void ComponentDiagramOptions_ShowRelationshipFlows_DefaultsToTrue()
    {
        var options = new ComponentDiagramOptions();
        Assert.True(options.ShowRelationshipFlows);
    }

    [Fact]
    public void ComponentDiagramOptions_ShowSystemFlameChart_DefaultsToTrue()
    {
        var options = new ComponentDiagramOptions();
        Assert.True(options.ShowSystemFlameChart);
    }

    // ── Phase 3: Browser SVG rendering ──

    [Fact]
    public void GenerateComponentDiagramReport_BrowserJs_Uses_PlantumlBrowserDiv()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = options,
                PlantUmlRendering = PlantUmlRendering.BrowserJs
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("plantuml-browser", html);
        Assert.Contains("data-plantuml", html);
        Assert.Contains("plantuml.js", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_Server_StillUsesImgTag()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = options,
                PlantUmlRendering = PlantUmlRendering.Server
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<img src=", html);
    }

    // ── Phase 3: Stats wired into diagram labels ──

    [Fact]
    public void GenerateComponentDiagramReport_WithTimestamps_IncludesStatsInPlantUml()
    {
        var reqId = Guid.NewGuid();
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var req = new RequestResponseLog("T", "t1", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "OrderService", "Caller", RequestResponseType.Request, Guid.NewGuid(), reqId, false) { Timestamp = baseTime };
        var res = new RequestResponseLog("T", "t1", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "OrderService", "Caller", RequestResponseType.Response, Guid.NewGuid(), reqId, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(100) };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            [req, res], new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.BrowserJs
            });

        var puml = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("P50:", puml);
        Assert.Contains("P95:", puml);
    }

    // ── Phase 3: System flow — no Gantt, has stats table ──

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_NoGanttSection()
    {
        using var source = new ActivitySource("Tests.Phase3.NoGantt");
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
        var wholeTestSegments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-test-1"] = new(Guid.Empty, RequestResponseType.Request, "test-1",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMilliseconds(50), [span])
        };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions { ShowSystemFlameChart = true } },
            wholeTestSegments: wholeTestSegments);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("@startgantt", html);
        Assert.DoesNotContain("system-gantt", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_HasPerformanceSummaryTable()
    {
        var reqId = Guid.NewGuid();
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        using var source = new ActivitySource("Tests.Phase3.StatsTable");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        using var span = source.StartActivity("Process")!;
        span.SetStartTime(baseTime.UtcDateTime);
        span.SetEndTime(baseTime.UtcDateTime.AddMilliseconds(50));

        var req = new RequestResponseLog("T", "t1", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "OrderService", "Caller", RequestResponseType.Request, Guid.NewGuid(), reqId, false) { Timestamp = baseTime };
        var res = new RequestResponseLog("T", "t1", HttpMethod.Get, null, new Uri("http://sut/api"),
            [], "OrderService", "Caller", RequestResponseType.Response, Guid.NewGuid(), reqId, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(100) };

        var wholeTestSegments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-test-t1"] = new(Guid.Empty, RequestResponseType.Request, "t1",
                baseTime, baseTime.AddMilliseconds(50), [span])
        };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            [req, res],
            new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions { ShowSystemFlameChart = true } },
            wholeTestSegments: wholeTestSegments);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("performance-summary", html);
        Assert.Contains("Mean", html);
        Assert.Contains("P95", html);
    }

    // ── Phase 4: Focus mode ──

    [Fact]
    public void GenerateComponentDiagramReport_BrowserJs_IncludesFocusModeScript()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.BrowserJs
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("focus-dimmed", html);
        Assert.Contains("focusNode", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_Server_DoesNotIncludeFocusScript()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.Server
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("focusNode", html);
    }
}
