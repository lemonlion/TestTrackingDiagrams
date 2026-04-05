using System.Diagnostics;
using System.Text;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Selenium;

/// <summary>
/// Generates minimal self-contained HTML test pages using the real
/// DiagramContextMenu scripts/styles and InternalFlowHtmlGenerator output.
/// </summary>
public static class TestPageGenerator
{
    /// <summary>
    /// Creates a test page with iflow popup system, fake segment data, and a clickable trigger button.
    /// </summary>
    public static string GenerateIflowPopupTestPage(
        bool includeToggle = false,
        bool includeCallTree = false,
        bool includeEmptySegment = false)
    {
        using var activitySource = new ActivitySource("TestTrackingDiagrams.Tests.Selenium");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        var parentSpan = activitySource.StartActivity("HTTP GET /api/orders", ActivityKind.Server)!;
        parentSpan.SetStartTime(DateTime.UtcNow);
        parentSpan.SetEndTime(DateTime.UtcNow.AddMilliseconds(150));

        var childSpan = activitySource.StartActivity("SELECT * FROM Orders", ActivityKind.Client,
            new ActivityContext(parentSpan.TraceId, parentSpan.SpanId, ActivityTraceFlags.Recorded))!;
        childSpan.SetStartTime(DateTime.UtcNow.AddMilliseconds(20));
        childSpan.SetEndTime(DateTime.UtcNow.AddMilliseconds(80));

        var segments = new Dictionary<string, InternalFlowSegment>
        {
            ["iflow-seg-1"] = new(
                Guid.NewGuid(), RequestResponseType.Request, "test-1",
                parentSpan.StartTimeUtc, parentSpan.StartTimeUtc + parentSpan.Duration,
                [parentSpan, childSpan])
        };

        if (includeEmptySegment)
        {
            segments["iflow-seg-empty"] = new(
                Guid.NewGuid(), RequestResponseType.Request, "test-2",
                null, null, []);
        }

        var diagramStyle = includeCallTree
            ? InternalFlowDiagramStyle.CallTree
            : InternalFlowDiagramStyle.ActivityDiagram;

        var segmentScript = InternalFlowHtmlGenerator.GenerateSegmentDataScript(
            segments, diagramStyle,
            showFlameChart: includeToggle,
            flameChartPosition: InternalFlowFlameChartPosition.BehindWithToggle);

        var styles = DiagramContextMenu.GetInternalFlowPopupStyles();
        var popupScript = DiagramContextMenu.GetInternalFlowPopupScript();
        var plantUmlBrowserScript = diagramStyle == InternalFlowDiagramStyle.ActivityDiagram
            ? DiagramContextMenu.GetPlantUmlBrowserRenderScript()
            : "";

        parentSpan.Dispose();
        childSpan.Dispose();

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Selenium Test Page</title>
                <style>
                    {{styles}}
                </style>
                {{plantUmlBrowserScript}}
                <style>
                    body { font-family: sans-serif; padding: 20px; }
                    .test-trigger { padding: 10px 20px; cursor: pointer; margin: 5px; }
                </style>
            </head>
            <body>
                <h1 id="page-title">iflow Popup Test Page</h1>

                <button id="trigger-seg-1" class="test-trigger"
                    onclick="window._iflowShowPopup('iflow-seg-1')">
                    Open Segment 1
                </button>

                <button id="trigger-seg-empty" class="test-trigger"
                    onclick="window._iflowShowPopup('iflow-seg-empty')">
                    Open Empty Segment
                </button>

                <button id="trigger-seg-missing" class="test-trigger"
                    onclick="window._iflowShowPopup('iflow-nonexistent')">
                    Open Missing Segment
                </button>

                {{segmentScript}}
                {{popupScript}}
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Creates a test page showing the whole-test flow details block with toggle.
    /// </summary>
    public static string GenerateWholeTestFlowPage(WholeTestFlowVisualization visualization = WholeTestFlowVisualization.Both)
    {
        using var activitySource = new ActivitySource("TestTrackingDiagrams.Tests.Selenium.WholeTest");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        var span1 = activitySource.StartActivity("HTTP GET /api/orders", ActivityKind.Server)!;
        span1.SetStartTime(DateTime.UtcNow);
        span1.SetEndTime(DateTime.UtcNow.AddMilliseconds(150));

        var span2 = activitySource.StartActivity("SELECT * FROM Orders", ActivityKind.Client,
            new ActivityContext(span1.TraceId, span1.SpanId, ActivityTraceFlags.Recorded))!;
        span2.SetStartTime(DateTime.UtcNow.AddMilliseconds(20));
        span2.SetEndTime(DateTime.UtcNow.AddMilliseconds(80));

        var testId = "test-whole-1";
        var segments = new Dictionary<string, InternalFlowSegment>
        {
            [$"iflow-test-{testId}"] = new(
                Guid.Empty, RequestResponseType.Request, testId,
                span1.StartTimeUtc, span1.StartTimeUtc + span1.Duration,
                [span1, span2])
        };

        var boundaryLogs = new[]
        {
            ("GET: /api/orders", new DateTimeOffset(span1.StartTimeUtc.AddMilliseconds(5), TimeSpan.Zero))
        };

        var flowHtml = InternalFlowHtmlGenerator.GenerateWholeTestFlowHtml(
            segments, testId, boundaryLogs, visualization);

        var styles = DiagramContextMenu.GetInternalFlowPopupStyles();
        var toggleScript = DiagramContextMenu.GetToggleScript();
        var plantUmlBrowserScript = visualization is WholeTestFlowVisualization.ActivityDiagram or WholeTestFlowVisualization.Both
            ? DiagramContextMenu.GetPlantUmlBrowserRenderScript()
            : "";

        span1.Dispose();
        span2.Dispose();

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Whole Test Flow Test Page</title>
                <style>
                    {{styles}}
                </style>
                {{plantUmlBrowserScript}}
                {{toggleScript}}
                <style>
                    body { font-family: sans-serif; padding: 20px; }
                </style>
            </head>
            <body>
                <h1 id="page-title">Whole Test Flow Test Page</h1>
                {{flowHtml}}
            </body>
            </html>
            """;
    }

    /// <summary>
    /// Creates a test page simulating the component diagram with relationship flow list and popups.
    /// </summary>
    public static string GenerateComponentFlowPage()
    {
        using var activitySource = new ActivitySource("TestTrackingDiagrams.Tests.Selenium.CompFlow");
        using var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        Activity.Current = null;
        var span = activitySource.StartActivity("DB Query", ActivityKind.Client)!;
        span.SetStartTime(DateTime.UtcNow);
        span.SetEndTime(DateTime.UtcNow.AddMilliseconds(50));

        var segment = new InternalFlowSegment(
            Guid.Empty, RequestResponseType.Request, "aggregated",
            span.StartTimeUtc, span.StartTimeUtc + span.Duration,
            [span]);

        var flowData = new RelationshipFlowData(segment, [
            new RelationshipTestSummary("t1", "Order Creation Test", 1, 45),
            new RelationshipTestSummary("t2", "Payment Flow Test", 1, 23)
        ]);

        var popupContent = InternalFlowHtmlGenerator.GenerateRelationshipPopupContent(
            flowData, InternalFlowDiagramStyle.ActivityDiagram);

        var popupData = new Dictionary<string, object>
        {
            ["iflow-rel-API-DB"] = new { title = "API → DB", content = popupContent }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(popupData,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        var dataScript = $"<script>window.__iflowSegments = {json};</script>";

        var styles = DiagramContextMenu.GetInternalFlowPopupStyles();
        var plantUmlBrowserScript = DiagramContextMenu.GetPlantUmlBrowserRenderScript();
        var popupScript = DiagramContextMenu.GetInternalFlowPopupScript();
        var toggleScript = DiagramContextMenu.GetToggleScript();

        span.Dispose();

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Component Flow Test Page</title>
                <style>
                    {{styles}}
                </style>
                {{plantUmlBrowserScript}}
                <style>
                    body { font-family: sans-serif; padding: 20px; }
                    .iflow-rel-list { list-style: none; padding: 0; margin: 16px 0; }
                    .iflow-rel-list li { padding: 6px 12px; border: 1px solid #e0e0e0; border-radius: 4px; margin: 4px 0; cursor: pointer; }
                    .iflow-rel-list li:hover { background: #f0f4ff; border-color: #4285f4; }
                    .iflow-rel-summary-table { width: 100%; border-collapse: collapse; margin-top: 12px; }
                    .iflow-rel-summary-table th { text-align: left; padding: 4px 8px; border-bottom: 2px solid #ddd; }
                    .iflow-rel-summary-table td { padding: 4px 8px; border-bottom: 1px solid #eee; }
                </style>
            </head>
            <body>
                <h1 id="page-title">Component Flow Test Page</h1>

                <h2>Relationship Flows</h2>
                <ul class="iflow-rel-list">
                    <li id="rel-api-db" onclick="window._iflowShowPopup('iflow-rel-API-DB')">
                        API → DB (1 span, 2 tests)
                    </li>
                </ul>

                {{dataScript}}
                {{popupScript}}
                {{toggleScript}}
            </body>
            </html>
            """;
    }
}
