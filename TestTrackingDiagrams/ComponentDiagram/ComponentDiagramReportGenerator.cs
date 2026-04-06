using System.Text;
using System.Text.Json;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ComponentDiagram;

public static class ComponentDiagramReportGenerator
{
    public record ComponentDiagramResult(string PumlFilePath, string HtmlFilePath, string PlantUml);

    public static ComponentDiagramResult GenerateComponentDiagramReport(
        IEnumerable<RequestResponseLog> logs,
        ReportConfigurationOptions reportOptions,
        Dictionary<string, InternalFlowSegment>? perBoundarySegments = null,
        Dictionary<string, InternalFlowSegment>? wholeTestSegments = null)
    {
        var options = reportOptions.ComponentDiagramOptions ?? new ComponentDiagramOptions();
        var plantUmlServerBaseUrl = reportOptions.PlantUmlServerBaseUrl;
        var imageFormat = reportOptions.PlantUmlImageFormat;
        var localDiagramRenderer = reportOptions.PlantUmlRendering == PlantUmlRendering.Local
            ? reportOptions.LocalDiagramRenderer
            : null;
        var useBrowserJs = reportOptions.PlantUmlRendering == PlantUmlRendering.BrowserJs;

        var logsArray = logs as RequestResponseLog[] ?? logs.ToArray();
        var relationships = ComponentDiagramGenerator.ExtractRelationships(logsArray, options.ParticipantFilter);

        // Compute stats from request/response timestamp pairs
        var stats = ComponentFlowSegmentBuilder.ComputeRelationshipStats(
            relationships, logsArray, options.LowCoverageThreshold);

        var plantUml = ComponentDiagramGenerator.GeneratePlantUml(relationships, options, stats: stats.Count > 0 ? stats : null);

        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);

        var pumlPath = Path.Combine(directory, $"{options.FileName}.puml");
        File.WriteAllText(pumlPath, plantUml);

        // Build flow data
        Dictionary<string, RelationshipFlowData>? relationshipFlows = null;

        if (options.ShowRelationshipFlows && perBoundarySegments is { Count: > 0 })
        {
            relationshipFlows = ComponentFlowSegmentBuilder.BuildRelationshipSegments(
                relationships, logsArray, perBoundarySegments);
        }

        var imgSrc = useBrowserJs
            ? null
            : GetImageSource(plantUml, plantUmlServerBaseUrl, imageFormat, localDiagramRenderer, directory, options.FileName);

        var html = GenerateHtml(plantUml, options.Title, imgSrc, imageFormat,
            relationships, relationshipFlows, stats, wholeTestSegments,
            options.RelationshipFlowStyle,
            reportOptions.InternalFlowHasDataBehavior,
            useBrowserJs,
            options.MaxFlameChartTests);
        var htmlPath = Path.Combine(directory, $"{options.FileName}.html");
        File.WriteAllText(htmlPath, html);

        return new ComponentDiagramResult(pumlPath, htmlPath, plantUml);
    }

    private static string GetImageSource(
        string plantUml,
        string plantUmlServerBaseUrl,
        PlantUmlImageFormat imageFormat,
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer,
        string directory,
        string fileName)
    {
        if (localDiagramRenderer is not null)
        {
            var renderFormat = imageFormat switch
            {
                PlantUmlImageFormat.Base64Png => PlantUmlImageFormat.Png,
                PlantUmlImageFormat.Base64Svg => PlantUmlImageFormat.Svg,
                _ => imageFormat
            };
            var imageBytes = localDiagramRenderer(plantUml, renderFormat);
            var isBase64 = imageFormat is PlantUmlImageFormat.Base64Png or PlantUmlImageFormat.Base64Svg;

            if (isBase64)
            {
                var mimeType = renderFormat == PlantUmlImageFormat.Png ? "image/png" : "image/svg+xml";
                return $"data:{mimeType};base64,{Convert.ToBase64String(imageBytes)}";
            }

            var extension = renderFormat == PlantUmlImageFormat.Png ? ".png" : ".svg";
            var imageFileName = $"{fileName}{extension}";
            File.WriteAllBytes(Path.Combine(directory, imageFileName), imageBytes);
            return imageFileName;
        }

        var encoded = PlantUmlTextEncoder.Encode(plantUml);
        var formatPath = imageFormat switch
        {
            PlantUmlImageFormat.Svg or PlantUmlImageFormat.Base64Svg => "svg",
            _ => "png"
        };
        return $"{plantUmlServerBaseUrl}/{formatPath}/{encoded}";
    }

    private static string GenerateHtml(
        string plantUml,
        string title,
        string? imgSrc,
        PlantUmlImageFormat imageFormat,
        ComponentRelationship[] relationships,
        Dictionary<string, RelationshipFlowData>? relationshipFlows,
        Dictionary<string, RelationshipStats> stats,
        Dictionary<string, InternalFlowSegment>? wholeTestSegments,
        InternalFlowDiagramStyle flowStyle,
        InternalFlowHasDataBehavior hasDataBehavior = InternalFlowHasDataBehavior.ShowLinkOnHover,
        bool useBrowserJs = false,
        int maxFlameChartTests = 50)
    {
        var encodedPlantUml = System.Net.WebUtility.HtmlEncode(plantUml);

        // Diagram rendering: browser SVG or server <img>
        string diagramHtml;
        if (useBrowserJs)
        {
            diagramHtml = $"<div class=\"plantuml-browser\" id=\"comp-diagram\" data-plantuml=\"{encodedPlantUml}\" data-diagram-type=\"plantuml\">Loading...</div>";
        }
        else
        {
            diagramHtml = $"""<img src="{imgSrc}" alt="{title}" style="max-width: 100%;" />""";
        }

        var hasFlows = (relationshipFlows?.Count > 0) || (wholeTestSegments?.Count > 0) || (stats.Count > 0);

        // Build flow-specific HTML sections
        var flowStyles = "";
        var flowScripts = "";
        var flowDataScript = "";
        var relListHtml = "";
        var systemFlowHtml = "";

        // Always include browser render script when using BrowserJs
        if (useBrowserJs)
        {
            flowScripts = DiagramContextMenu.GetPlantUmlBrowserRenderScript()
                        + DiagramContextMenu.GetFocusModeScript();
        }

        if (hasFlows)
        {
            flowStyles = DiagramContextMenu.GetInternalFlowPopupStyles()
                       + DiagramContextMenu.GetStyles();
            flowScripts += DiagramContextMenu.GetInternalFlowPopupScript()
                        + DiagramContextMenu.GetFlameChartRenderScript()
                        + DiagramContextMenu.GetToggleScript()
                        + DiagramContextMenu.GetContextMenuScript();

            var popupData = new Dictionary<string, object>();

            // Relationship flows
            if (relationshipFlows?.Count > 0)
            {
                var relSb = new StringBuilder();
                relSb.AppendLine("<h2>Relationship Flows</h2>");
                relSb.AppendLine("<ul class=\"iflow-rel-list\">");

                foreach (var rel in relationships)
                {
                    var relKey = $"iflow-rel-{SanitizeKey(rel.Caller)}-{SanitizeKey(rel.Service)}";
                    if (!relationshipFlows.TryGetValue(relKey, out var flow))
                        continue;

                    var spanCount = flow.AggregatedSegment.Spans.Length;
                    var testCount = flow.TestSummaries.Length;
                    var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                    var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);

                    relSb.AppendLine($"<li onclick=\"window._iflowShowPopup('{relKey}')\">" +
                        $"{callerEnc} \u2192 {serviceEnc} ({spanCount} span{(spanCount == 1 ? "" : "s")}, {testCount} test{(testCount == 1 ? "" : "s")})</li>");

                    var content = InternalFlowHtmlGenerator.GenerateRelationshipPopupContent(flow, flowStyle);
                    popupData[relKey] = new
                    {
                        title = $"{rel.Caller} \u2192 {rel.Service}",
                        content
                    };
                }

                relSb.AppendLine("</ul>");
                relListHtml = relSb.ToString();
            }

            // System flow — performance summary table + capped flame chart (no Gantt)
            if (wholeTestSegments is { Count: > 0 } || stats.Count > 0)
            {
                var sysSb = new StringBuilder();
                sysSb.AppendLine("<h2>System Flow</h2>");

                // Performance summary table
                if (stats.Count > 0)
                {
                    sysSb.AppendLine("<h3>Performance Summary</h3>");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr><th>Relationship</th><th>Calls</th><th>Mean</th><th>P50</th><th>P95</th><th>P99</th><th>Errors</th></tr>");

                    foreach (var rel in relationships)
                    {
                        var relKey = $"iflow-rel-{SanitizeKey(rel.Caller)}-{SanitizeKey(rel.Service)}";
                        if (!stats.TryGetValue(relKey, out var relStats))
                            continue;

                        var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                        var errorStyle = relStats.ErrorRate > 0 ? " style=\"color:#c00;font-weight:bold\"" : "";

                        sysSb.AppendLine($"<tr><td>{callerEnc} \u2192 {serviceEnc}</td>" +
                            $"<td>{relStats.CallCount}</td>" +
                            $"<td>{relStats.MeanMs:F0}ms</td>" +
                            $"<td>{relStats.MedianMs:F0}ms</td>" +
                            $"<td>{relStats.P95Ms:F0}ms</td>" +
                            $"<td>{relStats.P99Ms:F0}ms</td>" +
                            $"<td{errorStyle}>{relStats.ErrorRate * 100:F0}%</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                }

                // Sequential flame chart (capped)
                if (wholeTestSegments is { Count: > 0 })
                {
                    // Cap to slowest N tests
                    var cappedSegments = wholeTestSegments
                        .OrderByDescending(kv => kv.Value.Spans.Length > 0
                            ? kv.Value.Spans.Max(s => s.StartTimeUtc + s.Duration) - kv.Value.Spans.Min(s => s.StartTimeUtc)
                            : TimeSpan.Zero)
                        .Take(maxFlameChartTests)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);

                    var seqData = InternalFlowRenderer.GetSequentialFlameChartData(cappedSegments);
                    var seqJson = JsonSerializer.Serialize(
                        new { s = seqData.Sources, b = seqData.Bands },
                        new JsonSerializerOptions { WriteIndented = false });
                    var encodedSeqJson = System.Net.WebUtility.HtmlEncode(seqJson);

                    var label = wholeTestSegments.Count > maxFlameChartTests
                        ? $"Flame Chart (top {maxFlameChartTests} of {wholeTestSegments.Count} tests by duration)"
                        : "Flame Chart";

                    sysSb.AppendLine($"<h3>{label}</h3>");
                    sysSb.AppendLine($"<div class=\"iflow-flame iflow-sequential-tests\" data-diagram-type=\"flamechart\" data-flame=\"{encodedSeqJson}\"></div>");
                }

                systemFlowHtml = sysSb.ToString();
            }

            if (popupData.Count > 0)
            {
                var json = JsonSerializer.Serialize(popupData, new JsonSerializerOptions { WriteIndented = false });
                flowDataScript = DiagramContextMenu.GetInternalFlowConfigScript(hasDataBehavior)
                    + $"<script>window.__iflowSegments = {json};</script>";
            }
        }

        return $$"""
                <html>
                    <head>
                        <style>
                            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 2rem; }
                            h1 { color: #333; }
                            h2 { color: #444; margin-top: 2rem; }
                            h3 { color: #555; margin-top: 1.5rem; }
                            pre { background: #f6f8fa; padding: 1rem; border-radius: 6px; overflow-x: auto; font-size: 0.85rem; }
                            .diagram-container { margin: 1rem 0; }
                            .diagram-image { margin: 1rem 0; text-align: center; }
                            .diagram-image img { max-width: 100%; height: auto; }
                            .performance-summary { border-collapse: collapse; width: 100%; margin: 1rem 0; }
                            .performance-summary th, .performance-summary td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                            .performance-summary th { background: #f6f8fa; font-weight: 600; }
                            .performance-summary tr:nth-child(even) { background: #fafbfc; }
                            {{flowStyles}}
                        </style>
                        {{flowDataScript}}
                        {{flowScripts}}
                    </head>
                    <body>
                        <h1>{{title}}</h1>
                        <div class="diagram-image">
                            {{diagramHtml}}
                        </div>
                        <div class="diagram-container">
                            <details>
                                <summary><strong>PlantUML Source</strong></summary>
                                <pre>{{encodedPlantUml}}</pre>
                            </details>
                        </div>
                        {{relListHtml}}
                        {{systemFlowHtml}}
                    </body>
                </html>
                """;
    }

    private static string SanitizeKey(string name) =>
        name.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
}
