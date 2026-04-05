using System.Diagnostics;
using System.Text;
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

        parentSpan.Dispose();
        childSpan.Dispose();

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <title>Selenium Test Page</title>
                {{styles}}
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
}
