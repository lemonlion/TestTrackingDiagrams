using System.Net;
using System.Text;

namespace TestTrackingDiagrams.Reports;

public static class CiSummaryInteractiveHtmlGenerator
{
    public static string GenerateHtml(
        Feature[] features,
        DefaultDiagramsFetcher.DiagramAsCode[] diagrams,
        DateTime startRunTime,
        DateTime endRunTime,
        int maxDiagrams = 10)
    {
        var sb = new StringBuilder();
        var allScenarios = features.SelectMany(f => f.Scenarios).ToArray();
        var passed = allScenarios.Count(s => s.Result == ScenarioResult.Passed);
        var failed = allScenarios.Count(s => s.Result == ScenarioResult.Failed);
        var skipped = allScenarios.Count(s => s.Result == ScenarioResult.Skipped);
        var total = allScenarios.Length;
        var hasFailed = failed > 0;
        var status = hasFailed ? "&#x274C; Failed" : "&#x2705; Passed";
        var duration = FormatDuration(endRunTime - startRunTime);
        var diagramsByTestId = diagrams.ToLookup(d => d.TestRuntimeId);

        sb.AppendLine("""
            <html>
            <head>
                <meta charset="utf-8">
                <script src="https://plantuml.github.io/plantuml/js-plantuml/viz-global.js"></script>
                <script src="https://plantuml.github.io/plantuml/js-plantuml/plantuml.js"></script>
                <style>
                    body { font-family: sans-serif; margin: 2em; }
                    table { border-collapse: collapse; margin-bottom: 1em; }
                    td, th { border: 1px solid #ddd; padding: 6px 12px; text-align: left; }
                    .error { color: #c00; }
                    .diagram { margin: 1em 0; }
                    details { margin: 0.5em 0; }
                </style>
            </head>
            <body>
            """);

        sb.AppendLine("<h1>Test Run Summary</h1>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><td>Status</td><td>{status}</td></tr>");
        sb.AppendLine($"<tr><td>Scenarios</td><td>{total}</td></tr>");
        sb.AppendLine($"<tr><td>Passed</td><td>{passed}</td></tr>");
        sb.AppendLine($"<tr><td>Failed</td><td>{failed}</td></tr>");
        sb.AppendLine($"<tr><td>Skipped</td><td>{skipped}</td></tr>");
        sb.AppendLine($"<tr><td>Duration</td><td>{duration}</td></tr>");
        sb.AppendLine("</table>");

        var diagramIndex = 0;

        if (hasFailed)
        {
            sb.AppendLine($"<h2>Failed Scenarios ({failed})</h2>");
            var shown = 0;
            foreach (var feature in features)
            {
                foreach (var scenario in feature.Scenarios.Where(s => s.Result == ScenarioResult.Failed))
                {
                    if (shown >= maxDiagrams) break;
                    sb.AppendLine($"<h3>{Encode(feature.DisplayName)} &mdash; {Encode(scenario.DisplayName)}</h3>");
                    if (!string.IsNullOrEmpty(scenario.ErrorMessage))
                        sb.AppendLine($"<p class=\"error\"><strong>Error:</strong> {Encode(scenario.ErrorMessage)}</p>");
                    if (!string.IsNullOrEmpty(scenario.ErrorStackTrace))
                    {
                        sb.AppendLine("<details><summary>Stack Trace</summary>");
                        sb.AppendLine($"<pre>{Encode(scenario.ErrorStackTrace)}</pre>");
                        sb.AppendLine("</details>");
                    }
                    AppendDiagrams(sb, diagramsByTestId[scenario.Id], ref diagramIndex);
                    shown++;
                }
                if (shown >= maxDiagrams) break;
            }
        }
        else
        {
            var scenariosWithDiagrams = features
                .SelectMany(f => f.Scenarios.Select(s => (Feature: f, Scenario: s)))
                .Where(x => diagramsByTestId[x.Scenario.Id].Any())
                .ToArray();

            if (scenariosWithDiagrams.Length > 0)
            {
                sb.AppendLine("<h2>Sequence Diagrams</h2>");
                var shown = 0;
                foreach (var (feature, scenario) in scenariosWithDiagrams)
                {
                    if (shown >= maxDiagrams) break;
                    sb.AppendLine($"<h3>{Encode(feature.DisplayName)} &mdash; {Encode(scenario.DisplayName)}</h3>");
                    AppendDiagrams(sb, diagramsByTestId[scenario.Id], ref diagramIndex);
                    shown++;
                }
            }
        }

        sb.AppendLine("""
            <script>
                plantumlLoad();
                var observer = new IntersectionObserver(function(entries) {
                    entries.forEach(function(entry) {
                        if (!entry.isIntersecting) return;
                        var el = entry.target;
                        if (el.dataset.rendered) return;
                        el.dataset.rendered = '1';
                        observer.unobserve(el);
                        var source = el.getAttribute('data-source');
                        var lines = source.split('\n');
                        plantuml.render(lines, el.id);
                    });
                }, { rootMargin: '200px' });
                document.querySelectorAll('.plantuml-diagram').forEach(function(el) {
                    observer.observe(el);
                });
            </script>
            </body>
            </html>
            """);

        return sb.ToString();
    }

    private static void AppendDiagrams(
        StringBuilder sb,
        IEnumerable<DefaultDiagramsFetcher.DiagramAsCode> diagrams,
        ref int index)
    {
        foreach (var diagram in diagrams)
        {
            var id = $"puml-{index++}";
            sb.AppendLine($"""<div class="diagram plantuml-diagram" id="{id}" data-source="{Encode(diagram.CodeBehind)}"></div>""");
        }
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);

    private static string FormatDuration(TimeSpan duration)
    {
        var total = duration.Duration();
        if (total.TotalSeconds < 1)
            return $"{total.Milliseconds}ms";
        if (total.TotalMinutes < 1)
            return $"{total.Seconds}s";
        return $"{(int)total.TotalMinutes}m {total.Seconds}s";
    }
}
