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
        var localDiagramRenderer = reportOptions.PlantUmlRendering switch
        {
            PlantUmlRendering.Local => reportOptions.LocalDiagramRenderer,
            PlantUmlRendering.NodeJs => PlantUml.NodeJsPlantUmlRenderer.Render,
            _ => null
        };
        var useBrowserJs = reportOptions.PlantUmlRendering == PlantUmlRendering.BrowserJs;

        var logsArray = logs as RequestResponseLog[] ?? logs.ToArray();
        var relationships = ComponentDiagramGenerator.ExtractRelationships(logsArray, options.ParticipantFilter);

        // Compute stats from request/response timestamp pairs
        var stats = ComponentFlowSegmentBuilder.ComputeRelationshipStats(
            relationships, logsArray, options.LowCoverageThreshold);

        // Compute call ordering patterns and error correlations
        var callOrderings = ComponentFlowSegmentBuilder.BuildCallOrdering(logsArray);
        var callOrderingPatterns = ComponentFlowSegmentBuilder.ComputeCallOrderingPatterns(callOrderings);
        var errorCorrelations = ComponentFlowSegmentBuilder.ComputeErrorCorrelations(relationships, logsArray);

        var plantUml = ComponentDiagramGenerator.GeneratePlantUml(relationships, options, stats: stats.Count > 0 ? stats : null, useC4: !useBrowserJs);

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
            options.MaxFlameChartTests,
            callOrderingPatterns,
            errorCorrelations);
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
        int maxFlameChartTests = 50,
        CallOrderingPattern[]? callOrderingPatterns = null,
        ErrorCorrelation[]? errorCorrelations = null)
    {
        // Diagram rendering: browser SVG or server <img>
        string diagramHtml;
        if (useBrowserJs)
        {
            var compressed = InternalFlow.InternalFlowHtmlGenerator.CompressToBase64(plantUml);
            diagramHtml = $"<div class=\"plantuml-browser\" id=\"comp-diagram\" data-plantuml-z=\"{compressed}\" data-diagram-type=\"plantuml\">Loading...</div>";
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
                        + DiagramContextMenu.GetToggleScript()
                        + DiagramContextMenu.GetContextMenuScript();

            // System flow — performance summary table + latency bar chart + insights
            if (stats.Count > 0)
            {
                var sysSb = new StringBuilder();
                sysSb.AppendLine("<h2>System Flow</h2>");

                // Sortable performance summary table with endpoint breakdowns
                sysSb.AppendLine("<h3>Performance Summary</h3>");
                sysSb.AppendLine("<table class=\"performance-summary sortable\">");
                sysSb.AppendLine("<tr>" +
                    "<th data-sort-col=\"0\" onclick=\"sortTable(this)\">Relationship &#x25C6;</th>" +
                    "<th data-sort-col=\"1\" onclick=\"sortTable(this)\">Calls &#x25C6;</th>" +
                    "<th data-sort-col=\"2\" onclick=\"sortTable(this)\">Mean &#x25C6;</th>" +
                    "<th data-sort-col=\"3\" onclick=\"sortTable(this)\">P50 &#x25C6;</th>" +
                    "<th data-sort-col=\"4\" onclick=\"sortTable(this)\">P95 &#x25C6;</th>" +
                    "<th data-sort-col=\"5\" onclick=\"sortTable(this)\">P99 &#x25C6;</th>" +
                    "<th data-sort-col=\"6\" onclick=\"sortTable(this)\" title=\"Coefficient of Variation (stdDev/mean). Low=consistent, High=variable\">CV &#x25C6;</th>" +
                    "<th data-sort-col=\"7\" onclick=\"sortTable(this)\">Errors &#x25C6;</th></tr>");

                foreach (var rel in relationships)
                {
                    var relKey = $"iflow-rel-{SanitizeKey(rel.Caller)}-{SanitizeKey(rel.Service)}";
                    if (!stats.TryGetValue(relKey, out var relStats))
                        continue;

                    var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                    var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                    var errorStyle = relStats.ErrorRate > 0 ? " style=\"color:#c00;font-weight:bold\"" : "";
                    var hasEndpoints = relStats.EndpointBreakdown.Length > 0;
                    var expandClass = hasEndpoints ? " class=\"expandable\"" : "";
                    var expandClick = hasEndpoints ? $" onclick=\"toggleEndpoints('{relKey}')\"" : "";
                    var expandIcon = hasEndpoints ? " &#x25B6;" : "";
                    var lowCovClass = relStats.IsLowCoverage ? " low-coverage" : "";
                    var cvClass = relStats.CoefficientOfVariation < 0.3 ? "cv-low" : relStats.CoefficientOfVariation > 0.7 ? "cv-high" : "cv-med";
                    var outlierBadge = relStats.Outliers is not null ? $" <span class=\"outlier-badge\" title=\"{relStats.Outliers.OutlierCount} outlier(s) detected\">&#x1F53A; {relStats.Outliers.OutlierCount}</span>" : "";

                    sysSb.AppendLine($"<tr{expandClass}{expandClick} data-rel=\"{relKey}\">" +
                        $"<td data-sort-value=\"{callerEnc} {serviceEnc}\">{expandIcon} {callerEnc} \u2192 {serviceEnc}{(relStats.IsLowCoverage ? " <span class=\"low-coverage\" title=\"Low test coverage\">\u26A0</span>" : "")}{outlierBadge}</td>" +
                        $"<td data-sort-value=\"{relStats.CallCount}\">{relStats.CallCount}</td>" +
                        $"<td data-sort-value=\"{relStats.MeanMs:F1}\">{relStats.MeanMs:F0}ms</td>" +
                        $"<td data-sort-value=\"{relStats.MedianMs:F1}\">{relStats.MedianMs:F0}ms</td>" +
                        $"<td data-sort-value=\"{relStats.P95Ms:F1}\">{relStats.P95Ms:F0}ms</td>" +
                        $"<td data-sort-value=\"{relStats.P99Ms:F1}\">{relStats.P99Ms:F0}ms</td>" +
                        $"<td data-sort-value=\"{relStats.CoefficientOfVariation:F3}\" class=\"{cvClass}\">{relStats.CoefficientOfVariation:F2}</td>" +
                        $"<td data-sort-value=\"{relStats.ErrorRate:F4}\"{errorStyle}>{relStats.ErrorRate * 100:F0}%</td></tr>");

                    // Endpoint breakdown rows (initially hidden)
                    if (hasEndpoints)
                    {
                        foreach (var ep in relStats.EndpointBreakdown)
                        {
                            var epErrorStyle = ep.ErrorRate > 0 ? " style=\"color:#c00;font-weight:bold\"" : "";
                            sysSb.AppendLine($"<tr class=\"endpoint-row\" data-parent=\"{relKey}\" style=\"display:none\">" +
                                $"<td style=\"padding-left:2rem;font-size:0.85em;color:#666\">{System.Net.WebUtility.HtmlEncode(ep.Method)} {System.Net.WebUtility.HtmlEncode(ep.Path)}</td>" +
                                $"<td>{ep.CallCount}</td>" +
                                $"<td>{ep.MeanMs:F0}ms</td>" +
                                $"<td>{ep.MedianMs:F0}ms</td>" +
                                $"<td>{ep.P95Ms:F0}ms</td>" +
                                $"<td>{ep.P99Ms:F0}ms</td>" +
                                $"<td></td>" +
                                $"<td{epErrorStyle}>{ep.ErrorRate * 100:F0}%</td></tr>");
                        }
                    }
                }

                sysSb.AppendLine("</table>");

                // Latency bar chart with percentile toggles
                sysSb.AppendLine("<h3>Latency Distribution</h3>");
                sysSb.AppendLine("<div class=\"percentile-toggles\">");
                sysSb.AppendLine("<button class=\"percentile-toggle\" data-metric=\"mean\" onclick=\"switchMetric(this)\">Mean</button>");
                sysSb.AppendLine("<button class=\"percentile-toggle\" data-metric=\"p50\" onclick=\"switchMetric(this)\">P50</button>");
                sysSb.AppendLine("<button class=\"percentile-toggle active\" data-metric=\"p95\" onclick=\"switchMetric(this)\">P95</button>");
                sysSb.AppendLine("<button class=\"percentile-toggle\" data-metric=\"p99\" onclick=\"switchMetric(this)\">P99</button>");
                sysSb.AppendLine("</div>");
                sysSb.AppendLine("<div class=\"latency-chart\">");

                foreach (var rel in relationships)
                {
                    var relKey = $"iflow-rel-{SanitizeKey(rel.Caller)}-{SanitizeKey(rel.Service)}";
                    if (!stats.TryGetValue(relKey, out var relStats))
                        continue;

                    var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                    var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);

                    sysSb.AppendLine($"<div class=\"latency-bar-row\" " +
                        $"data-mean=\"{relStats.MeanMs:F1}\" " +
                        $"data-p50=\"{relStats.MedianMs:F1}\" " +
                        $"data-p95=\"{relStats.P95Ms:F1}\" " +
                        $"data-p99=\"{relStats.P99Ms:F1}\">" +
                        $"<span class=\"latency-label\">{callerEnc} \u2192 {serviceEnc}</span>" +
                        $"<div class=\"latency-bar\"><div class=\"latency-fill\"></div><span class=\"latency-value\"></span></div>" +
                        $"</div>");
                }

                sysSb.AppendLine("</div>");

                // Status code distribution
                var relWithCodes = relationships
                    .Select(r => (Rel: r, Key: $"iflow-rel-{SanitizeKey(r.Caller)}-{SanitizeKey(r.Service)}"))
                    .Where(x => stats.TryGetValue(x.Key, out var s) && s.StatusCodeDistribution.Count > 0)
                    .ToArray();

                if (relWithCodes.Length > 0)
                {
                    sysSb.AppendLine("<h3>Status Codes</h3>");
                    sysSb.AppendLine("<div class=\"status-code-dist\">");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr><th>Relationship</th><th>Status Codes</th></tr>");

                    foreach (var (rel, relKey) in relWithCodes)
                    {
                        var relStats = stats[relKey];
                        var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                        var codeParts = relStats.StatusCodeDistribution
                            .OrderBy(kv => kv.Key)
                            .Select(kv =>
                            {
                                var code = (int)kv.Key;
                                var cls = code >= 500 ? "status-5xx" : code >= 400 ? "status-4xx" : "status-2xx";
                                return $"<span class=\"{cls}\">{code}: {kv.Value}</span>";
                            });
                        sysSb.AppendLine($"<tr><td>{callerEnc} \u2192 {serviceEnc}</td><td>{string.Join(" &nbsp; ", codeParts)}</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                    sysSb.AppendLine("</div>");
                }

                // Payload sizes
                var relWithPayloads = relationships
                    .Select(r => (Rel: r, Key: $"iflow-rel-{SanitizeKey(r.Caller)}-{SanitizeKey(r.Service)}"))
                    .Where(x => stats.TryGetValue(x.Key, out var s) && s.PayloadSizes is not null)
                    .ToArray();

                if (relWithPayloads.Length > 0)
                {
                    sysSb.AppendLine("<h3>Payload Sizes</h3>");
                    sysSb.AppendLine("<div class=\"payload-sizes\">");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr>" +
                        "<th data-sort-col=\"0\" onclick=\"sortTable(this)\">Relationship &#x25C6;</th>" +
                        "<th data-sort-col=\"1\" onclick=\"sortTable(this)\">Req Mean &#x25C6;</th>" +
                        "<th data-sort-col=\"2\" onclick=\"sortTable(this)\">Req P95 &#x25C6;</th>" +
                        "<th data-sort-col=\"3\" onclick=\"sortTable(this)\">Resp Mean &#x25C6;</th>" +
                        "<th data-sort-col=\"4\" onclick=\"sortTable(this)\">Resp P95 &#x25C6;</th></tr>");

                    foreach (var (rel, relKey) in relWithPayloads)
                    {
                        var ps = stats[relKey].PayloadSizes!;
                        var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                        sysSb.AppendLine($"<tr><td data-sort-value=\"{callerEnc} {serviceEnc}\">{callerEnc} \u2192 {serviceEnc}</td>" +
                            $"<td data-sort-value=\"{ps.RequestMeanBytes:F1}\">{FormatBytes(ps.RequestMeanBytes)}</td>" +
                            $"<td data-sort-value=\"{ps.RequestP95Bytes:F1}\">{FormatBytes(ps.RequestP95Bytes)}</td>" +
                            $"<td data-sort-value=\"{ps.ResponseMeanBytes:F1}\">{FormatBytes(ps.ResponseMeanBytes)}</td>" +
                            $"<td data-sort-value=\"{ps.ResponseP95Bytes:F1}\">{FormatBytes(ps.ResponseP95Bytes)}</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                    sysSb.AppendLine("</div>");
                }

                // Concurrency info
                var relWithConcurrency = relationships
                    .Select(r => (Rel: r, Key: $"iflow-rel-{SanitizeKey(r.Caller)}-{SanitizeKey(r.Service)}"))
                    .Where(x => stats.TryGetValue(x.Key, out var s) && s.Concurrency is not null)
                    .ToArray();

                if (relWithConcurrency.Length > 0)
                {
                    sysSb.AppendLine("<h3>Concurrent Calls</h3>");
                    sysSb.AppendLine("<div class=\"concurrency-info\">");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr><th>Relationship</th><th>Concurrent Tests</th><th>Percentage</th><th>With</th></tr>");

                    foreach (var (rel, relKey) in relWithConcurrency)
                    {
                        var ci = stats[relKey].Concurrency!;
                        var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                        sysSb.AppendLine($"<tr><td>{callerEnc} \u2192 {serviceEnc}</td>" +
                            $"<td>{ci.ConcurrentCallCount}</td>" +
                            $"<td>{ci.ConcurrencyPercentage:F0}%</td>" +
                            $"<td>{System.Net.WebUtility.HtmlEncode(string.Join(", ", ci.ConcurrentPairs))}</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                    sysSb.AppendLine("</div>");
                }

                // Request method distribution
                var relWithMethods = relationships
                    .Select(r => (Rel: r, Key: $"iflow-rel-{SanitizeKey(r.Caller)}-{SanitizeKey(r.Service)}"))
                    .Where(x => stats.TryGetValue(x.Key, out var s) && s.MethodDistribution.Count > 1)
                    .ToArray();

                if (relWithMethods.Length > 0)
                {
                    sysSb.AppendLine("<h3>Request Methods</h3>");
                    sysSb.AppendLine("<div class=\"method-dist\">");

                    foreach (var (rel, relKey) in relWithMethods)
                    {
                        var md = stats[relKey].MethodDistribution;
                        var total = md.Values.Sum();
                        var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);

                        sysSb.AppendLine($"<div class=\"method-row\">");
                        sysSb.AppendLine($"<span class=\"method-label\">{callerEnc} \u2192 {serviceEnc}</span>");
                        sysSb.AppendLine("<div class=\"method-bar\">");
                        foreach (var (method, count) in md.OrderByDescending(kv => kv.Value))
                        {
                            var pct = (double)count / total * 100;
                            var methodEnc = System.Net.WebUtility.HtmlEncode(method);
                            sysSb.AppendLine($"<div class=\"method-segment method-{methodEnc}\" style=\"width:{pct:F1}%\" title=\"{methodEnc}: {count}\">{methodEnc}</div>");
                        }
                        sysSb.AppendLine("</div></div>");
                    }

                    sysSb.AppendLine("</div>");
                }

                // Outlier detection
                var relWithOutliers = relationships
                    .Select(r => (Rel: r, Key: $"iflow-rel-{SanitizeKey(r.Caller)}-{SanitizeKey(r.Service)}"))
                    .Where(x => stats.TryGetValue(x.Key, out var s) && s.Outliers is not null)
                    .ToArray();

                if (relWithOutliers.Length > 0)
                {
                    sysSb.AppendLine("<h3>Outlier Detection</h3>");
                    sysSb.AppendLine("<div class=\"outlier-detection\">");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr><th>Relationship</th><th>Threshold</th><th>Outliers</th><th>Top Outlier Tests</th></tr>");

                    foreach (var (rel, relKey) in relWithOutliers)
                    {
                        var oi = stats[relKey].Outliers!;
                        var callerEnc = System.Net.WebUtility.HtmlEncode(rel.Caller);
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                        var topTests = string.Join(", ", oi.TopOutliers.Select(o =>
                            $"{System.Net.WebUtility.HtmlEncode(o.TestName)} ({o.DurationMs:F0}ms, {o.DeviationsFromMean:F1}\u03C3)"));
                        sysSb.AppendLine($"<tr><td>{callerEnc} \u2192 {serviceEnc}</td>" +
                            $"<td>{oi.ThresholdMs:F0}ms</td>" +
                            $"<td>{oi.OutlierCount}</td>" +
                            $"<td>{topTests}</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                    sysSb.AppendLine("</div>");
                }

                // Latency contribution breakdown
                var relWithContribution = relationships
                    .Select(r => (Rel: r, Key: $"iflow-rel-{SanitizeKey(r.Caller)}-{SanitizeKey(r.Service)}"))
                    .Where(x => stats.TryGetValue(x.Key, out var s) && s.LatencyContributionPct > 0)
                    .ToArray();

                if (relWithContribution.Length > 1)
                {
                    sysSb.AppendLine("<h3>Latency Contribution</h3>");
                    sysSb.AppendLine("<div class=\"contribution-chart\">");
                    sysSb.AppendLine("<div class=\"contribution-bar\">");

                    foreach (var (rel, relKey) in relWithContribution.OrderByDescending(x => stats[x.Key].LatencyContributionPct))
                    {
                        var pct = stats[relKey].LatencyContributionPct;
                        var serviceEnc = System.Net.WebUtility.HtmlEncode(rel.Service);
                        sysSb.AppendLine($"<div class=\"contribution-segment\" style=\"width:{pct:F1}%\" title=\"{serviceEnc}: {pct:F0}%\">{serviceEnc} {pct:F0}%</div>");
                    }

                    sysSb.AppendLine("</div></div>");
                }

                // Error correlations
                if (errorCorrelations is { Length: > 0 })
                {
                    sysSb.AppendLine("<h3>Error Correlations</h3>");
                    sysSb.AppendLine("<div class=\"error-correlations\">");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr><th>When this errors...</th><th>This also errors</th><th>Co-occurrence</th><th>Count</th></tr>");

                    foreach (var ec in errorCorrelations)
                    {
                        var relAEnc = System.Net.WebUtility.HtmlEncode(ec.RelationshipA);
                        var relBEnc = System.Net.WebUtility.HtmlEncode(ec.RelationshipB);
                        sysSb.AppendLine($"<tr><td>{relAEnc}</td><td>{relBEnc}</td>" +
                            $"<td>{ec.CoOccurrencePct:F0}%</td><td>{ec.CoOccurrenceCount}/{ec.TotalErrorTests}</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                    sysSb.AppendLine("</div>");
                }

                // Call ordering patterns
                if (callOrderingPatterns is { Length: > 0 })
                {
                    sysSb.AppendLine("<h3>Call Ordering</h3>");
                    sysSb.AppendLine("<div class=\"call-ordering\">");
                    sysSb.AppendLine("<table class=\"performance-summary\">");
                    sysSb.AppendLine("<tr><th>First Called</th><th>Then Called</th><th>Pattern</th><th>Tests</th></tr>");

                    foreach (var pattern in callOrderingPatterns)
                    {
                        var firstEnc = System.Net.WebUtility.HtmlEncode(pattern.FirstService);
                        var secondEnc = System.Net.WebUtility.HtmlEncode(pattern.SecondService);
                        sysSb.AppendLine($"<tr><td>{firstEnc}</td><td>{secondEnc}</td>" +
                            $"<td>{pattern.PctFirstBeforeSecond:F0}% first</td><td>{pattern.SampleCount}</td></tr>");
                    }

                    sysSb.AppendLine("</table>");
                    sysSb.AppendLine("</div>");
                }

                systemFlowHtml = sysSb.ToString();
            }
        }

        var sortScript = GetSortScript();
        var barChartScript = GetBarChartScript();
        var endpointToggleScript = GetEndpointToggleScript();

        return $$"""
                <html>
                    <head>
                        <meta charset="utf-8">
                        <style>
                            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 2rem; }
                            h1 { color: #333; }
                            h2 { color: #444; margin-top: 2rem; }
                            h3 { color: #555; margin-top: 1.5rem; }
                            pre { background: #f6f8fa; padding: 1rem; border-radius: 6px; overflow-x: auto; font-size: 0.85rem; }
                            .diagram-image { margin: 1rem 0; text-align: center; }
                            .diagram-image img { max-width: 100%; height: auto; }
                            .performance-summary { border-collapse: collapse; width: 100%; margin: 1rem 0; }
                            .performance-summary th, .performance-summary td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                            .performance-summary th { background: #f6f8fa; font-weight: 600; cursor: pointer; user-select: none; }
                            .performance-summary th:hover { background: #e8ecf0; }
                            .performance-summary tr:nth-child(even):not(.endpoint-row) { background: #fafbfc; }
                            .performance-summary tr.expandable { cursor: pointer; }
                            .performance-summary tr.expandable:hover { background: #eef2f7; }
                            .endpoint-row { background: #f9fafb; }
                            .endpoint-row td { font-style: italic; }
                            .low-coverage { color: #d97706; }
                            .percentile-toggles { margin: 0.5rem 0; }
                            .percentile-toggle { padding: 6px 16px; margin-right: 4px; border: 1px solid #ddd; background: #f6f8fa; border-radius: 4px; cursor: pointer; font-size: 0.85rem; }
                            .percentile-toggle.active { background: #0366d6; color: #fff; border-color: #0366d6; }
                            .latency-chart { margin: 1rem 0; }
                            .latency-bar-row { display: flex; align-items: center; margin: 4px 0; }
                            .latency-label { width: 250px; min-width: 250px; text-align: right; padding-right: 12px; font-size: 0.85rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
                            .latency-bar { flex: 1; height: 24px; background: #f0f0f0; border-radius: 3px; position: relative; overflow: hidden; }
                            .latency-fill { height: 100%; background: #0366d6; border-radius: 3px; transition: width 0.3s; }
                            .latency-value { position: absolute; right: 8px; top: 3px; font-size: 0.8rem; color: #333; }
                            .status-2xx { color: #22863a; font-weight: 600; }
                            .status-4xx { color: #d97706; font-weight: 600; }
                            .status-5xx { color: #c00; font-weight: 600; }
                            .cv-low { color: #22863a; font-weight: 600; }
                            .cv-med { color: #d97706; font-weight: 600; }
                            .cv-high { color: #c00; font-weight: 600; }
                            .outlier-badge { color: #c00; font-size: 0.8rem; }
                            .method-dist { margin: 1rem 0; }
                            .method-row { display: flex; align-items: center; margin: 4px 0; }
                            .method-label { width: 250px; min-width: 250px; text-align: right; padding-right: 12px; font-size: 0.85rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
                            .method-bar { display: flex; flex: 1; height: 24px; border-radius: 3px; overflow: hidden; }
                            .method-segment { display: flex; align-items: center; justify-content: center; font-size: 0.75rem; color: #fff; min-width: 30px; }
                            .method-GET { background: #0366d6; }
                            .method-POST { background: #22863a; }
                            .method-PUT { background: #d97706; }
                            .method-DELETE { background: #c00; }
                            .method-PATCH { background: #6f42c1; }
                            .contribution-chart { margin: 1rem 0; }
                            .contribution-bar { display: flex; height: 32px; border-radius: 4px; overflow: hidden; background: #f0f0f0; }
                            .contribution-segment { display: flex; align-items: center; justify-content: center; font-size: 0.75rem; color: #fff; background: #0366d6; border-right: 1px solid #fff; min-width: 40px; }
                            .contribution-segment:nth-child(2n) { background: #6f42c1; }
                            .contribution-segment:nth-child(3n) { background: #22863a; }
                            {{flowStyles}}
                        </style>
                        {{flowDataScript}}
                        {{flowScripts}}
                        {{sortScript}}
                        {{barChartScript}}
                        {{endpointToggleScript}}
                    </head>
                    <body>
                        <h1>{{title}}</h1>
                        <div class="diagram-image">
                            {{diagramHtml}}
                        </div>
                        {{systemFlowHtml}}
                    </body>
                </html>
                """;
    }

    private static string GetSortScript() => """
        <script>
        function sortTable(th) {
            var table = th.closest('table');
            var col = parseInt(th.getAttribute('data-sort-col'));
            var rows = Array.from(table.querySelectorAll('tr')).slice(1).filter(r => !r.classList.contains('endpoint-row'));
            var asc = th.getAttribute('data-sort-dir') !== 'asc';
            th.setAttribute('data-sort-dir', asc ? 'asc' : 'desc');
            // Reset other headers
            table.querySelectorAll('th[data-sort-col]').forEach(h => { if (h !== th) h.removeAttribute('data-sort-dir'); });
            rows.sort(function(a, b) {
                var aCell = a.cells[col], bCell = b.cells[col];
                var aVal = aCell.getAttribute('data-sort-value') || aCell.textContent;
                var bVal = bCell.getAttribute('data-sort-value') || bCell.textContent;
                var aN = parseFloat(aVal), bN = parseFloat(bVal);
                var cmp = (!isNaN(aN) && !isNaN(bN)) ? aN - bN : aVal.localeCompare(bVal);
                return asc ? cmp : -cmp;
            });
            var tbody = table.querySelector('tbody') || table;
            rows.forEach(function(row) {
                var relKey = row.getAttribute('data-rel');
                tbody.appendChild(row);
                if (relKey) {
                    table.querySelectorAll('.endpoint-row[data-parent="' + relKey + '"]').forEach(function(ep) { tbody.appendChild(ep); });
                }
            });
        }
        </script>
        """;

    private static string GetBarChartScript() => """
        <script>
        function switchMetric(btn) {
            btn.parentElement.querySelectorAll('.percentile-toggle').forEach(b => b.classList.remove('active'));
            btn.classList.add('active');
            var metric = btn.getAttribute('data-metric');
            renderBars(metric);
        }
        function renderBars(metric) {
            var rows = document.querySelectorAll('.latency-bar-row');
            var maxVal = 0;
            rows.forEach(function(r) { var v = parseFloat(r.getAttribute('data-' + metric) || '0'); if (v > maxVal) maxVal = v; });
            rows.forEach(function(r) {
                var v = parseFloat(r.getAttribute('data-' + metric) || '0');
                var pct = maxVal > 0 ? (v / maxVal * 100) : 0;
                var fill = r.querySelector('.latency-fill');
                var val = r.querySelector('.latency-value');
                if (fill) fill.style.width = pct + '%';
                if (val) val.textContent = v.toFixed(0) + 'ms';
            });
        }
        document.addEventListener('DOMContentLoaded', function() { renderBars('p95'); });
        </script>
        """;

    private static string GetEndpointToggleScript() => """
        <script>
        function toggleEndpoints(relKey) {
            var rows = document.querySelectorAll('.endpoint-row[data-parent="' + relKey + '"]');
            var parentRow = document.querySelector('tr[data-rel="' + relKey + '"]');
            var show = rows.length > 0 && rows[0].style.display === 'none';
            rows.forEach(function(r) { r.style.display = show ? '' : 'none'; });
        }
        </script>
        """;

    private static string FormatBytes(double bytes)
    {
        if (bytes < 1024) return $"{bytes:F0} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1} KB";
        return $"{bytes / (1024 * 1024):F1} MB";
    }

    private static string SanitizeKey(string name) =>
        name.Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
}
