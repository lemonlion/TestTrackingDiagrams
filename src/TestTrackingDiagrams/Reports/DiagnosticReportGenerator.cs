using System.Text;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Reports;

public static class DiagnosticReportGenerator
{
    public static void Generate(
        RequestResponseLog[] logs,
        Feature[] features,
        ReportConfigurationOptions options)
    {
        var html = BuildHtml(logs, features, options);
        var directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, options.ReportsFolderPath);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "DiagnosticReport.html"), html);
    }

    internal static string BuildHtml(
        RequestResponseLog[] logs,
        Feature[] features,
        ReportConfigurationOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<title>TTD Diagnostic Report</title>");
        sb.AppendLine("<style>body{font-family:system-ui,sans-serif;margin:2em;color:#333}table{border-collapse:collapse;margin:1em 0}th,td{border:1px solid #ddd;padding:6px 12px;text-align:left}th{background:#f5f5f5}h2{margin-top:1.5em;border-bottom:1px solid #ddd;padding-bottom:4px}.warn{color:#b45309}.info{color:#1d4ed8}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine("<h1>TestTrackingDiagrams — Diagnostic Report</h1>");

        // Configuration dump
        sb.AppendLine("<h2>Configuration</h2><table>");
        AppendRow(sb, "InternalFlowTracking", options.InternalFlowTracking);
        AppendRow(sb, "InternalFlowSpanGranularity", options.InternalFlowSpanGranularity);
        AppendRow(sb, "InternalFlowActivitySources", options.InternalFlowActivitySources is { Length: > 0 } ? string.Join(", ", options.InternalFlowActivitySources) : "<not configured>");
        AppendRow(sb, "InternalFlowDiagramStyle", options.InternalFlowDiagramStyle);
        AppendRow(sb, "InternalFlowNoDataBehavior", options.InternalFlowNoDataBehavior);
        AppendRow(sb, "DiagramFormat", options.DiagramFormat);
        AppendRow(sb, "PlantUmlRendering", options.PlantUmlRendering);
        AppendRow(sb, "GenerateComponentDiagram", options.GenerateComponentDiagram);
        sb.AppendLine("</table>");

        // Log summary
        var distinctTestIds = logs.Select(l => l.TestId).Distinct().ToArray();
        sb.AppendLine("<h2>Request/Response Log Summary</h2>");
        sb.AppendLine($"<p>{logs.Length} total log entries across {distinctTestIds.Length} distinct test ID(s).</p>");

        // Entries per service
        var byService = logs.GroupBy(l => l.ServiceName).OrderByDescending(g => g.Count()).ToArray();
        if (byService.Length > 0)
        {
            sb.AppendLine("<h3>Entries per Service</h3><table><tr><th>Service</th><th>Count</th></tr>");
            foreach (var g in byService)
                sb.AppendLine($"<tr><td>{Escape(g.Key)}</td><td>{g.Count()}</td></tr>");
            sb.AppendLine("</table>");
        }

        // Entries per test
        var byTest = logs.GroupBy(l => l.TestId).OrderByDescending(g => g.Count()).ToArray();
        if (byTest.Length > 0)
        {
            sb.AppendLine("<h3>Entries per Test (top 50)</h3><table><tr><th>Test Name</th><th>Test ID</th><th>Count</th></tr>");
            foreach (var g in byTest.Take(50))
            {
                var name = g.First().TestName;
                sb.AppendLine($"<tr><td>{Escape(name)}</td><td>{Escape(g.Key)}</td><td>{g.Count()}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        // Unpaired requests
        var requests = logs.Where(l => l.Type == RequestResponseType.Request).ToArray();
        var responseIds = logs.Where(l => l.Type == RequestResponseType.Response).Select(l => l.RequestResponseId).ToHashSet();
        var unpaired = requests.Where(r => !responseIds.Contains(r.RequestResponseId)).ToArray();
        if (unpaired.Length > 0)
        {
            sb.AppendLine($"<h3 class=\"warn\">⚠ {unpaired.Length} Unpaired Request(s)</h3><table><tr><th>Test</th><th>Service</th><th>Method</th><th>URI</th></tr>");
            foreach (var r in unpaired.Take(50))
                sb.AppendLine($"<tr><td>{Escape(r.TestName)}</td><td>{Escape(r.ServiceName)}</td><td>{r.Method.Value}</td><td>{Escape(r.Uri.ToString())}</td></tr>");
            sb.AppendLine("</table>");
        }

        // Orphaned test IDs
        if (features.Length > 0)
        {
            var scenarioIds = features.SelectMany(f => f.Scenarios).Select(s => s.Id).ToHashSet();
            var orphaned = distinctTestIds.Where(id => !scenarioIds.Contains(id)).ToArray();
            if (orphaned.Length > 0)
            {
                sb.AppendLine($"<h3 class=\"warn\">⚠ {orphaned.Length} Orphaned Test ID(s)</h3>");
                sb.AppendLine("<p>These test IDs appear in logs but don't match any feature scenario:</p><ul>");
                foreach (var id in orphaned.Take(50))
                {
                    var name = logs.FirstOrDefault(l => l.TestId == id)?.TestName ?? "?";
                    sb.AppendLine($"<li>{Escape(name)} ({Escape(id)})</li>");
                }
                sb.AppendLine("</ul>");
            }

            var testsWithNoDiagrams = features.SelectMany(f => f.Scenarios)
                .Where(s => !logs.Any(l => l.TestId == s.Id))
                .ToArray();
            if (testsWithNoDiagrams.Length > 0)
            {
                sb.AppendLine($"<h3 class=\"warn\">⚠ {testsWithNoDiagrams.Length} Scenario(s) with No Log Entries</h3><ul>");
                foreach (var s in testsWithNoDiagrams.Take(50))
                    sb.AppendLine($"<li>{Escape(s.DisplayName)}</li>");
                sb.AppendLine("</ul>");
            }
        }

        // Activity span summary
        var allSpans = InternalFlowSpanStore.GetSpans();
        sb.AppendLine($"<h2>Activity Spans</h2><p>{allSpans.Length} total span(s) in InternalFlowSpanStore.</p>");

        var sources = ActivitySourceDiscovery.GetDiscoveredSources();
        if (sources.Count > 0)
        {
            var tracked = options.InternalFlowActivitySources?.ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];
            sb.AppendLine("<h3>Discovered Activity Sources</h3><table><tr><th>Source</th><th>Spans</th><th>Status</th></tr>");
            foreach (var (name, count) in sources.OrderByDescending(s => s.Value))
            {
                var isTracked = tracked.Count == 0 || tracked.Contains(name);
                var wellKnown = InternalFlowSpanCollector.WellKnownAutoInstrumentationSources.Contains(name);
                var status = isTracked ? "<span class=\"info\">✓ tracked</span>" : wellKnown ? "well-known (auto)" : "not tracked";
                sb.AppendLine($"<tr><td>{Escape(name)}</td><td>{count}</td><td>{status}</td></tr>");
            }
            sb.AppendLine("</table>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private static void AppendRow(StringBuilder sb, string key, object? value)
    {
        sb.AppendLine($"<tr><td><code>{Escape(key)}</code></td><td>{Escape(value?.ToString() ?? "<null>")}</td></tr>");
    }

    private static string Escape(string s) => System.Net.WebUtility.HtmlEncode(s);
}
