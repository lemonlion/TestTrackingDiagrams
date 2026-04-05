using System.Text;
using System.Text.Json;
using TestTrackingDiagrams.ComponentDiagram;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// Generates the JSON data block that the popup JavaScript reads
/// to display internal flow diagrams.
/// </summary>
public static class InternalFlowHtmlGenerator
{
    /// <summary>
    /// Generates a &lt;script&gt; block that populates <c>window.__iflowSegments</c>
    /// with segment data for the popup to consume.
    /// </summary>
    public static string GenerateSegmentDataScript(
        Dictionary<string, InternalFlowSegment> segments,
        InternalFlowDiagramStyle diagramStyle,
        bool showFlameChart = false,
        InternalFlowFlameChartPosition flameChartPosition = InternalFlowFlameChartPosition.BehindWithToggle)
    {
        var data = new Dictionary<string, object>();

        foreach (var (key, segment) in segments)
        {
            if (segment.Spans.Length > 0)
            {
                var mainContent = diagramStyle switch
                {
                    InternalFlowDiagramStyle.CallTree => InternalFlowRenderer.RenderCallTree(segment),
                    InternalFlowDiagramStyle.ActivityDiagram => RenderActivityDiagramHtml(segment),
                    _ => RenderActivityDiagramHtml(segment)
                };

                var content = mainContent;
                if (showFlameChart)
                {
                    var flameHtml = InternalFlowRenderer.RenderFlameChart(segment);
                    content = flameChartPosition switch
                    {
                        InternalFlowFlameChartPosition.Underneath =>
                            mainContent + "<hr style=\"margin:12px 0\">" + flameHtml,
                        _ => // BehindWithToggle
                            "<div class=\"iflow-toggle\">"
                            + "<button class=\"iflow-toggle-btn iflow-toggle-active\" data-view=\"main\">Activity</button>"
                            + "<button class=\"iflow-toggle-btn\" data-view=\"flame\">Flame Chart</button>"
                            + "</div>"
                            + "<div class=\"iflow-view iflow-view-main\">" + mainContent + "</div>"
                            + "<div class=\"iflow-view iflow-view-flame\" style=\"display:none\">" + flameHtml + "</div>"
                    };
                }

                data[key] = new
                {
                    title = $"Internal Flow ({segment.Spans.Length} span{(segment.Spans.Length == 1 ? "" : "s")})",
                    content
                };
            }
            else
            {
                data[key] = new
                {
                    message = "No internal activity captured for this segment."
                };
            }
        }

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = false });
        return $"<script>window.__iflowSegments = {json};</script>";
    }

    private static string RenderActivityDiagramHtml(InternalFlowSegment segment)
    {
        var plantuml = InternalFlowRenderer.RenderActivityDiagram(segment);
        var id = $"iflow-puml-{segment.RequestResponseId}-{segment.BoundaryType.ToString().ToLowerInvariant()}";
        var encoded = System.Net.WebUtility.HtmlEncode(plantuml);
        return $"<div class=\"plantuml-browser iflow-diagram\" id=\"{id}\" data-plantuml=\"{encoded}\" data-diagram-type=\"plantuml\">Loading...</div>";
    }

    /// <summary>
    /// Generates an inline collapsed &lt;details&gt; block containing the whole-test
    /// flamechart and/or activity diagram for a specific test.
    /// </summary>
    public static string GenerateWholeTestFlowHtml(
        Dictionary<string, InternalFlowSegment> wholeTestSegments,
        string testId,
        (string Label, DateTimeOffset Timestamp)[] boundaryLogs,
        WholeTestFlowVisualization visualization)
    {
        if (visualization == WholeTestFlowVisualization.None)
            return string.Empty;

        var segmentKey = $"iflow-test-{testId}";
        if (!wholeTestSegments.TryGetValue(segmentKey, out var segment) || segment.Spans.Length == 0)
            return string.Empty;

        var spanCount = segment.Spans.Length;
        var sb = new StringBuilder();
        sb.AppendLine($"<details class=\"whole-test-flow\">");
        sb.AppendLine($"<summary class=\"h4\">Whole Test Flow ({spanCount} span{(spanCount == 1 ? "" : "s")})</summary>");

        switch (visualization)
        {
            case WholeTestFlowVisualization.FlameChart:
                sb.Append(InternalFlowRenderer.RenderFlameChartWithBoundaryMarkers(segment, boundaryLogs));
                break;

            case WholeTestFlowVisualization.ActivityDiagram:
                sb.Append(RenderWholeTestActivityDiagramHtml(segment));
                break;

            case WholeTestFlowVisualization.Both:
                var activityHtml = RenderWholeTestActivityDiagramHtml(segment);
                var flameHtml = InternalFlowRenderer.RenderFlameChartWithBoundaryMarkers(segment, boundaryLogs);
                sb.Append("<div class=\"iflow-toggle\">");
                sb.Append("<button class=\"iflow-toggle-btn iflow-toggle-active\" data-view=\"main\">Activity</button>");
                sb.Append("<button class=\"iflow-toggle-btn\" data-view=\"flame\">Flame Chart</button>");
                sb.Append("</div>");
                sb.Append($"<div class=\"iflow-view iflow-view-main\">{activityHtml}</div>");
                sb.Append($"<div class=\"iflow-view iflow-view-flame\" style=\"display:none\">{flameHtml}</div>");
                break;
        }

        sb.AppendLine("</details>");
        return sb.ToString();
    }

    private static string RenderWholeTestActivityDiagramHtml(InternalFlowSegment segment)
    {
        var plantuml = InternalFlowRenderer.RenderActivityDiagram(segment);
        var id = $"iflow-puml-whole-{segment.TestId}";
        var encoded = System.Net.WebUtility.HtmlEncode(plantuml);
        return $"<div class=\"plantuml-browser iflow-diagram\" id=\"{id}\" data-plantuml=\"{encoded}\" data-diagram-type=\"plantuml\">Loading...</div>";
    }

    /// <summary>
    /// Generates HTML content for a relationship flow popup, including the aggregated
    /// flow diagram and a summary table of tests sorted by duration.
    /// </summary>
    public static string GenerateRelationshipPopupContent(
        RelationshipFlowData flowData,
        InternalFlowDiagramStyle style)
    {
        var sb = new StringBuilder();

        if (style == InternalFlowDiagramStyle.CallTree)
            sb.Append(InternalFlowRenderer.RenderCallTree(flowData.AggregatedSegment));
        else
            sb.Append(RenderActivityDiagramHtml(flowData.AggregatedSegment));

        sb.AppendLine("<table class=\"iflow-rel-summary-table\">");
        sb.AppendLine("<tr><th>Test</th><th>Spans</th><th>Duration</th></tr>");

        var top20 = flowData.TestSummaries.Take(20);
        foreach (var summary in top20)
        {
            var name = System.Net.WebUtility.HtmlEncode(summary.TestName);
            sb.AppendLine($"<tr><td>{name}</td><td>{summary.SpanCount}</td><td>{summary.DurationMs:F0}ms</td></tr>");
        }

        if (flowData.TestSummaries.Length > 20)
            sb.AppendLine($"<tr><td colspan=\"3\" style=\"color:#888;font-style:italic\">...and {flowData.TestSummaries.Length - 20} more tests</td></tr>");

        sb.AppendLine("</table>");
        return sb.ToString();
    }
}
