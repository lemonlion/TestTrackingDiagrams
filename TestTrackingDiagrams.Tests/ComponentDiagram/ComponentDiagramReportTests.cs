using System.Diagnostics;
using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

[Collection("DiagramsFetcher")]
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
        Assert.Contains("data-plantuml-z=", content);
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
        // Default is BrowserJs which uses plain PlantUML
        Assert.Contains("rectangle", puml);
        Assert.Contains("WebApp", puml);
        Assert.Contains("OrderService", puml);
        Assert.Contains("PaymentService", puml);
        Assert.Contains("-->", puml);
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
        // BrowserJs default: diagram source is compressed in data-plantuml-z
        Assert.Contains("data-plantuml-z=", html);
        // BrowserJs uses plain PlantUML with skinparams (verify via result.PlantUml)
        Assert.Contains("skinparam", result.PlantUml);
        Assert.DoesNotContain("C4_Context", result.PlantUml);
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
    public void GenerateComponentDiagramReport_PumlFile_UsesPlainPlantUmlByDefault()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, new ReportConfigurationOptions { ComponentDiagramOptions = options });

        var puml = File.ReadAllText(result.PumlFilePath);
        // Default BrowserJs mode uses plain PlantUML, no C4
        Assert.DoesNotContain("!include", puml);
        Assert.Contains("skinparam", puml);
        Assert.Contains("rectangle", puml);
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
        Assert.Contains("WebApp", result.PlantUml); // diagram source is now compressed in HTML
        Assert.Contains("OrderService", result.PlantUml);
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
        // Relationship flows section removed — popup infrastructure should not be populated
        Assert.DoesNotContain("<h2>Relationship Flows</h2>", html);
        Assert.DoesNotContain("<ul class=\"iflow-rel-list\"", html);
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
        // Without stats (no request/response timestamp pairs), System Flow section is not rendered
        // wholeTestSegments alone no longer triggers the flame chart (removed)
        Assert.DoesNotContain("class=\"iflow-flame iflow-sequential-tests\"", html);
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

    [Fact]
    public void GenerateComponentDiagramReport_BrowserJs_DoesNotContainC4Include()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.BrowserJs
            });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("!include", html);
        Assert.DoesNotContain("C4_Context", html);
        Assert.DoesNotContain("Person(", html);
        Assert.DoesNotContain("System(", html);
        // Should contain the browser-compatible skinparam styling (verify via PlantUml output)
        Assert.Contains("skinparam", result.PlantUml);
        Assert.Contains("rectangle", result.PlantUml);
    }

    [Fact]
    public void GenerateComponentDiagramReport_Server_ContainsC4Include()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions
            {
                ComponentDiagramOptions = new ComponentDiagramOptions(),
                PlantUmlRendering = PlantUmlRendering.Server
            });

        var puml = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("!include <C4/C4_Context>", puml);
        Assert.Contains("Person(", puml);
    }

    // ═══════════════════════════════════════════════════════════
    // Left-to-right direction
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_PlantUml_ContainsLeftToRight()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var puml = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("left to right direction", puml);
    }

    // ═══════════════════════════════════════════════════════════
    // PlantUML source section removed
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_Html_DoesNotContainPlantUmlSourceSection()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("<summary><strong>PlantUML Source</strong></summary>", html);
        Assert.DoesNotContain("diagram-container", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Sortable performance summary table
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_HasSortableHeaders()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("sortable", html);
        Assert.Contains("data-sort-col", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_IncludesSortScript()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("sortTable", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_HeadersHaveSortDataAttributes()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        // Each column header should have a data-sort-col attribute with its index
        Assert.Contains("data-sort-col=\"0\"", html); // Relationship
        Assert.Contains("data-sort-col=\"1\"", html); // Calls
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_CellsHaveDataSortValues()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("data-sort-value=", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Endpoint breakdown expandable rows
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_HasEndpointBreakdownRows()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("endpoint-row", html);
        Assert.Contains("/api", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_EndpointRowsInitiallyHidden()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("endpoint-row", html);
        Assert.Contains("display:none", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_RelationshipRowIsExpandable()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("expandable", html);
        Assert.Contains("toggleEndpoints", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Bar chart replaces flame chart
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_NoFlameChart()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        // No flame chart <div> elements in body (CSS rules in <style> don't count)
        Assert.DoesNotContain("class=\"iflow-flame iflow-sequential-tests\"", html);
        Assert.DoesNotContain("data-flame=\"", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_HasLatencyBarChart()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("latency-chart", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_BarChartHasPercentileToggles()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("percentile-toggle", html);
        Assert.Contains("data-metric=\"mean\"", html);
        Assert.Contains("data-metric=\"p50\"", html);
        Assert.Contains("data-metric=\"p95\"", html);
        Assert.Contains("data-metric=\"p99\"", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_BarChartHasDataAttributes()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("data-mean=", html);
        Assert.Contains("data-p50=", html);
        Assert.Contains("data-p95=", html);
        Assert.Contains("data-p99=", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_SystemFlow_P95SelectedByDefault()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        // The P95 toggle button should have an active class by default
        Assert.Contains("data-metric=\"p95\" onclick=\"switchMetric(this)\">P95</button>", html);
        Assert.Contains("class=\"percentile-toggle active\" data-metric=\"p95\"", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Relationship Flows replaced with useful data sections
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_NoRelationshipFlowsList()
    {
        var (span, perBoundary, logs) = CreateFlowTestData();
        using var _ = span;

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs,
            new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() },
            perBoundarySegments: perBoundary);

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("<h2>Relationship Flows</h2>", html);
        Assert.DoesNotContain("<ul class=\"iflow-rel-list\"", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HasStatusCodeDistribution()
    {
        var (logs, stats) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("status-code-dist", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_StatusCodeShowsDistribution()
    {
        var (logs, stats) = CreateStatsWithErrorsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("200", html);
        Assert.Contains("500", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HasPayloadSizeSection()
    {
        var (logs, stats) = CreateStatsWithPayloadTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("payload-sizes", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HasConcurrencySection()
    {
        var (logs, stats) = CreateStatsWithConcurrencyTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("concurrency-info", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_HasLowCoverageWarning()
    {
        var (logs, stats) = CreateStatsTestData(testCount: 1);

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("low-coverage", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Test data helpers for stats-based tests
    // ═══════════════════════════════════════════════════════════

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsTestData(int testCount = 5)
    {
        var reqId = Guid.NewGuid();
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        var logs = new List<RequestResponseLog>();
        for (int i = 0; i < testCount; i++)
        {
            var id = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, "request body",
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id, false) { Timestamp = baseTime.AddMilliseconds(i * 200) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, "response body",
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 200 + 100) });
        }

        // Force stats computation
        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithErrorsTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        // Successful calls
        for (int i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id, false) { Timestamp = baseTime.AddMilliseconds(i * 200) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 200 + 100) });
        }

        // Error call
        var errId = Guid.NewGuid();
        logs.Add(new RequestResponseLog("TestErr", "terr", HttpMethod.Post, null,
            new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
            RequestResponseType.Request, Guid.NewGuid(), errId, false) { Timestamp = baseTime.AddMilliseconds(800) });
        logs.Add(new RequestResponseLog("TestErr", "terr", HttpMethod.Post, null,
            new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
            RequestResponseType.Response, Guid.NewGuid(), errId, false, HttpStatusCode.InternalServerError) { Timestamp = baseTime.AddMilliseconds(900) });

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithPayloadTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        for (int i = 0; i < 3; i++)
        {
            var id = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Post,
                new string('x', 1024), // 1KB request body
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id, false) { Timestamp = baseTime.AddMilliseconds(i * 200) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Post,
                new string('y', 2048), // 2KB response body
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 200 + 100) });
        }

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithConcurrencyTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        // Two overlapping calls from same caller in same test
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Call 1: starts at t=0, ends at t=200
        logs.Add(new RequestResponseLog("Test1", "t1", HttpMethod.Get, null,
            new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
            RequestResponseType.Request, Guid.NewGuid(), id1, false) { Timestamp = baseTime });
        logs.Add(new RequestResponseLog("Test1", "t1", HttpMethod.Get, null,
            new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
            RequestResponseType.Response, Guid.NewGuid(), id1, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(200) });

        // Call 2: starts at t=50 (overlaps!), ends at t=150
        logs.Add(new RequestResponseLog("Test1", "t1", HttpMethod.Get, null,
            new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
            RequestResponseType.Request, Guid.NewGuid(), id2, false) { Timestamp = baseTime.AddMilliseconds(50) });
        logs.Add(new RequestResponseLog("Test1", "t1", HttpMethod.Get, null,
            new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
            RequestResponseType.Response, Guid.NewGuid(), id2, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(150) });

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    // ═══════════════════════════════════════════════════════════
    // Feature 6: Latency Variance (Coefficient of Variation)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_HasCVColumn()
    {
        var (logs, _) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains(">CV &#x25C6;</th>", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_LowCVGetsGreenStyling()
    {
        // All calls at same duration → CV ≈ 0 → green
        var (logs, _) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("cv-low", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PerformanceTable_HighCVGetsRedStyling()
    {
        var (logs, _) = CreateStatsWithHighVarianceTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("cv-high", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Feature 7: Request Method Distribution
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_MethodDistribution_ShowsMixedMethods()
    {
        var (logs, _) = CreateStatsWithMixedMethodsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("Request Methods", html);
        Assert.Contains("method-bar", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_MethodDistribution_NotShownWhenAllSameMethod()
    {
        var (logs, _) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("Request Methods", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_MethodDistribution_ColorsMatchMethod()
    {
        var (logs, _) = CreateStatsWithMixedMethodsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("method-GET", html);
        Assert.Contains("method-POST", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Feature 2: Outlier Detection
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_OutlierDetection_FlagsTestsBeyondTwoSigma()
    {
        var (logs, _) = CreateStatsWithOutlierTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("Outlier Detection", html);
        Assert.Contains("outlier", html.ToLower());
    }

    [Fact]
    public void GenerateComponentDiagramReport_OutlierDetection_NotShownWhenAllNormal()
    {
        var (logs, _) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("Outlier Detection", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_OutlierDetection_SkippedWhenTooFewCalls()
    {
        // Only 3 calls — not enough for outlier detection (needs ≥5)
        var (logs, _) = CreateStatsTestData(testCount: 3);

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("Outlier Detection", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Feature 1: Latency Contribution Breakdown
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_LatencyContribution_ShowsPercentagePerRelationship()
    {
        var (logs, _) = CreateMultiRelationshipTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("Latency Contribution", html);
        Assert.Contains("contribution-bar", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_LatencyContribution_NotRenderedWhenNoStats()
    {
        var logs = new[] { MakeRequest() };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("Latency Contribution", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Feature 5: Error Correlation
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_ErrorCorrelation_DetectsCoOccurringErrors()
    {
        var (logs, _) = CreateStatsWithCorrelatedErrorsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("Error Correlations", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_ErrorCorrelation_NotShownWhenNoErrors()
    {
        var (logs, _) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("Error Correlations", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Feature 3: Call Ordering Patterns
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GenerateComponentDiagramReport_CallOrdering_ShowsDominantPattern()
    {
        var (logs, _) = CreateMultiRelationshipTestData(testCount: 5);

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("Call Ordering", html);
    }

    [Fact]
    public void GenerateComponentDiagramReport_CallOrdering_NotShownWhenSingleService()
    {
        var (logs, _) = CreateStatsTestData();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(
            logs, new ReportConfigurationOptions { ComponentDiagramOptions = new ComponentDiagramOptions() });

        var html = File.ReadAllText(result.HtmlFilePath);
        Assert.DoesNotContain("Call Ordering", html);
    }

    // ═══════════════════════════════════════════════════════════
    // Test data helpers for new features
    // ═══════════════════════════════════════════════════════════

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithHighVarianceTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        // Mix of very fast and very slow calls → high CV
        var durations = new[] { 10, 20, 15, 500, 12, 18, 450, 25, 480, 10 };
        for (int i = 0; i < durations.Length; i++)
        {
            var id = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id, false) { Timestamp = baseTime.AddMilliseconds(i * 1000) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 1000 + durations[i]) });
        }

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithMixedMethodsTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        // Mix of GET and POST calls
        var methods = new[] { "GET", "GET", "POST", "POST", "GET" };
        for (int i = 0; i < methods.Length; i++)
        {
            var id = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Parse(methods[i]), null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id, false) { Timestamp = baseTime.AddMilliseconds(i * 200) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Parse(methods[i]), null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 200 + 100) });
        }

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithOutlierTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        // 9 normal calls at ~100ms, 1 outlier at 2000ms
        for (int i = 0; i < 10; i++)
        {
            var id = Guid.NewGuid();
            var duration = i == 9 ? 2000 : 100;
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id, false) { Timestamp = baseTime.AddMilliseconds(i * 3000) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 3000 + duration) });
        }

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateMultiRelationshipTestData(int testCount = 5)
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        for (int i = 0; i < testCount; i++)
        {
            // Each test calls OrderService (slow: 200ms) then PaymentService (fast: 50ms)
            var id1 = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id1, false) { Timestamp = baseTime.AddMilliseconds(i * 1000) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id1, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 200) });

            var id2 = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id2, false) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 300) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id2, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 350) });
        }

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }

    private (RequestResponseLog[] logs, Dictionary<string, RelationshipStats> stats) CreateStatsWithCorrelatedErrorsTestData()
    {
        var baseTime = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var logs = new List<RequestResponseLog>();

        // 3 tests where both OrderService and PaymentService error together
        for (int i = 0; i < 3; i++)
        {
            var id1 = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id1, false) { Timestamp = baseTime.AddMilliseconds(i * 1000) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id1, false, HttpStatusCode.InternalServerError) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 100) });

            var id2 = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id2, false) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 200) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id2, false, HttpStatusCode.InternalServerError) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 300) });
        }

        // 2 successful tests for both services
        for (int i = 3; i < 5; i++)
        {
            var id1 = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id1, false) { Timestamp = baseTime.AddMilliseconds(i * 1000) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/orders"), [], "OrderService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id1, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 100) });

            var id2 = Guid.NewGuid();
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
                RequestResponseType.Request, Guid.NewGuid(), id2, false) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 200) });
            logs.Add(new RequestResponseLog($"Test{i}", $"t{i}", HttpMethod.Get, null,
                new Uri("http://sut/api/payments"), [], "PaymentService", "Caller",
                RequestResponseType.Response, Guid.NewGuid(), id2, false, HttpStatusCode.OK) { Timestamp = baseTime.AddMilliseconds(i * 1000 + 300) });
        }

        return (logs.ToArray(), new Dictionary<string, RelationshipStats>());
    }
}
