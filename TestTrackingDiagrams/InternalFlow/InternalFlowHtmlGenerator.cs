using System.Text;
using System.Text.Json;

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
}
