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

        var logsArray = logs as RequestResponseLog[] ?? logs.ToArray();
        var relationships = ComponentDiagramGenerator.ExtractRelationships(logsArray, options.ParticipantFilter);
        var plantUml = ComponentDiagramGenerator.GeneratePlantUml(relationships, options);

        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(directory);

        var pumlPath = Path.Combine(directory, $"{options.FileName}.puml");
        File.WriteAllText(pumlPath, plantUml);

        // Build flow data
        Dictionary<string, RelationshipFlowData>? relationshipFlows = null;
        InternalFlowSegment? systemSegment = null;

        if (options.ShowRelationshipFlows && perBoundarySegments is { Count: > 0 })
        {
            relationshipFlows = ComponentFlowSegmentBuilder.BuildRelationshipSegments(
                relationships, logsArray, perBoundarySegments);
        }

        if (options.ShowSystemFlameChart && wholeTestSegments is { Count: > 0 })
        {
            systemSegment = ComponentFlowSegmentBuilder.BuildSystemSegment(wholeTestSegments);
        }

        var imgSrc = GetImageSource(plantUml, plantUmlServerBaseUrl, imageFormat, localDiagramRenderer, directory, options.FileName);
        var html = GenerateHtml(plantUml, options.Title, imgSrc, imageFormat,
            relationships, relationshipFlows, systemSegment, wholeTestSegments,
            options.RelationshipFlowStyle);
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
        string imgSrc,
        PlantUmlImageFormat imageFormat,
        ComponentRelationship[] relationships,
        Dictionary<string, RelationshipFlowData>? relationshipFlows,
        InternalFlowSegment? systemSegment,
        Dictionary<string, InternalFlowSegment>? wholeTestSegments,
        InternalFlowDiagramStyle flowStyle)
    {
        var imgTag = $"""<img src="{imgSrc}" alt="{title}" style="max-width: 100%;" />""";
        var encodedPlantUml = System.Net.WebUtility.HtmlEncode(plantUml);

        var hasFlows = (relationshipFlows?.Count > 0) || (systemSegment?.Spans.Length > 0);

        // Build flow-specific HTML sections
        var flowStyles = "";
        var flowScripts = "";
        var flowDataScript = "";
        var relListHtml = "";
        var systemFlowHtml = "";

        if (hasFlows)
        {
            flowStyles = DiagramContextMenu.GetInternalFlowPopupStyles()
                       + DiagramContextMenu.GetStyles();
            flowScripts = DiagramContextMenu.GetPlantUmlBrowserRenderScript()
                        + DiagramContextMenu.GetInternalFlowPopupScript()
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

            // System flow
            if (systemSegment?.Spans.Length > 0 && wholeTestSegments != null)
            {
                var sysSb = new StringBuilder();
                sysSb.AppendLine("<h2>System Flow</h2>");
                sysSb.AppendLine("<div class=\"iflow-toggle\">");
                sysSb.AppendLine("<button class=\"iflow-toggle-btn iflow-toggle-active\" data-view=\"flame\">Flame Chart</button>");
                sysSb.AppendLine("<button class=\"iflow-toggle-btn\" data-view=\"gantt\">Gantt</button>");
                sysSb.AppendLine("</div>");

                var flameHtml = InternalFlowRenderer.RenderSequentialTestFlameChart(wholeTestSegments);
                var ganttPuml = InternalFlowRenderer.RenderGantt(systemSegment);
                var ganttId = "iflow-puml-system-gantt";
                var ganttEncoded = System.Net.WebUtility.HtmlEncode(ganttPuml);

                sysSb.AppendLine($"<div class=\"iflow-view iflow-view-flame\">{flameHtml}</div>");
                sysSb.AppendLine($"<div class=\"iflow-view iflow-view-gantt\" style=\"display:none\">");
                sysSb.AppendLine($"<div class=\"plantuml-browser\" id=\"{ganttId}\" data-plantuml=\"{ganttEncoded}\" data-diagram-type=\"plantuml\">Loading...</div>");
                sysSb.AppendLine("</div>");

                systemFlowHtml = sysSb.ToString();
            }

            if (popupData.Count > 0)
            {
                var json = JsonSerializer.Serialize(popupData, new JsonSerializerOptions { WriteIndented = false });
                flowDataScript = $"<script>window.__iflowSegments = {json};</script>";
            }
        }

        return $$"""
                <html>
                    <head>
                        <style>
                            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 2rem; }
                            h1 { color: #333; }
                            h2 { color: #444; margin-top: 2rem; }
                            pre { background: #f6f8fa; padding: 1rem; border-radius: 6px; overflow-x: auto; font-size: 0.85rem; }
                            .diagram-container { margin: 1rem 0; }
                            .diagram-image { margin: 1rem 0; text-align: center; }
                            .diagram-image img { max-width: 100%; height: auto; }
                            {{flowStyles}}
                        </style>
                        {{flowScripts}}
                        {{flowDataScript}}
                    </head>
                    <body>
                        <h1>{{title}}</h1>
                        <div class="diagram-image">
                            {{imgTag}}
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
