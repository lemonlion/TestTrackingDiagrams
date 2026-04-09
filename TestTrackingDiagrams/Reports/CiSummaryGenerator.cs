using System.Text;
using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Reports;

public static class CiSummaryGenerator
{
    public static string GenerateMarkdown(
        Feature[] features,
        DefaultDiagramsFetcher.DiagramAsCode[] diagrams,
        DateTime startRunTime,
        DateTime endRunTime,
        int maxDiagrams = 10,
        DiagramFormat diagramFormat = DiagramFormat.PlantUml,
        PlantUmlRendering ciSummaryPlantUmlRendering = PlantUmlRendering.BrowserJs,
        string plantUmlServerBaseUrl = "https://plantuml.com/plantuml",
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer = null)
    {
        var sb = new StringBuilder();
        var allScenarios = features.SelectMany(f => f.Scenarios).ToArray();
        var passed = allScenarios.Count(s => s.Result == ScenarioResult.Passed);
        var failed = allScenarios.Count(s => s.Result == ScenarioResult.Failed);
        var skipped = allScenarios.Count(s => s.Result == ScenarioResult.Skipped);
        var total = allScenarios.Length;
        var hasFailed = failed > 0;
        var status = hasFailed ? "❌ Failed" : "✅ Passed";
        var duration = FormatDuration(endRunTime - startRunTime);

        sb.AppendLine("# Test Run Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|---|---|");
        sb.AppendLine($"| Status | {status} |");
        sb.AppendLine($"| Scenarios | {total} |");
        sb.AppendLine($"| Passed | {passed} |");
        sb.AppendLine($"| Failed | {failed} |");
        sb.AppendLine($"| Skipped | {skipped} |");
        sb.AppendLine($"| Duration | {duration} |");
        sb.AppendLine();

        var diagramsByTestId = diagrams.ToLookup(d => d.TestRuntimeId);

        if (hasFailed)
            AppendFailedScenarios(sb, features, diagramsByTestId, failed, maxDiagrams, diagramFormat, ciSummaryPlantUmlRendering, plantUmlServerBaseUrl, localDiagramRenderer);
        else
            AppendPassedDiagrams(sb, features, diagramsByTestId, total, maxDiagrams, diagramFormat, ciSummaryPlantUmlRendering, plantUmlServerBaseUrl, localDiagramRenderer);

        return sb.ToString();
    }

    private static void AppendFailedScenarios(
        StringBuilder sb,
        Feature[] features,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> diagramsByTestId,
        int totalFailed,
        int maxDiagrams,
        DiagramFormat diagramFormat,
        PlantUmlRendering ciSummaryPlantUmlRendering,
        string plantUmlServerBaseUrl,
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer)
    {
        sb.AppendLine($"## ❌ Failed Scenarios ({totalFailed})");
        sb.AppendLine();

        var shown = 0;
        foreach (var feature in features)
        {
            var failedScenarios = feature.Scenarios.Where(s => s.Result == ScenarioResult.Failed);
            foreach (var scenario in failedScenarios)
            {
                if (shown >= maxDiagrams) break;

                sb.AppendLine($"<details open><summary><strong>{EscapeHtml(feature.DisplayName)} — {EscapeHtml(scenario.DisplayName)}</strong></summary>");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(scenario.ErrorMessage))
                {
                    sb.AppendLine($"**Error:** {EscapeMarkdown(scenario.ErrorMessage)}");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(scenario.ErrorStackTrace))
                {
                    sb.AppendLine("<details><summary>Stack Trace</summary>");
                    sb.AppendLine();
                    sb.AppendLine("```");
                    sb.AppendLine(scenario.ErrorStackTrace);
                    sb.AppendLine("```");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                    sb.AppendLine();
                }

                AppendDiagramImages(sb, diagramsByTestId[scenario.Id], diagramFormat, ciSummaryPlantUmlRendering, plantUmlServerBaseUrl, localDiagramRenderer);

                sb.AppendLine("</details>");
                sb.AppendLine();
                shown++;
            }
            if (shown >= maxDiagrams) break;
        }

        var remaining = totalFailed - shown;
        if (remaining > 0)
        {
            sb.AppendLine($"*{remaining} more failed scenario(s) not shown — see full report*");
            sb.AppendLine();
        }
    }

    private static void AppendPassedDiagrams(
        StringBuilder sb,
        Feature[] features,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> diagramsByTestId,
        int totalScenarios,
        int maxDiagrams,
        DiagramFormat diagramFormat,
        PlantUmlRendering ciSummaryPlantUmlRendering,
        string plantUmlServerBaseUrl,
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer)
    {
        var scenariosWithDiagrams = features
            .SelectMany(f => f.Scenarios.Select(s => (Feature: f, Scenario: s)))
            .Where(x => diagramsByTestId[x.Scenario.Id].Any())
            .ToArray();

        if (scenariosWithDiagrams.Length == 0) return;

        sb.AppendLine("## Sequence Diagrams");
        sb.AppendLine();

        var shown = 0;
        foreach (var (feature, scenario) in scenariosWithDiagrams)
        {
            if (shown >= maxDiagrams) break;

            sb.AppendLine($"<details><summary><strong>{EscapeHtml(feature.DisplayName)} — {EscapeHtml(scenario.DisplayName)}</strong></summary>");
            sb.AppendLine();
            AppendDiagramImages(sb, diagramsByTestId[scenario.Id], diagramFormat, ciSummaryPlantUmlRendering, plantUmlServerBaseUrl, localDiagramRenderer);

            sb.AppendLine("</details>");
            sb.AppendLine();
            shown++;
        }

        var remaining = totalScenarios - shown;
        if (remaining > 0)
        {
            sb.AppendLine($"*{remaining} more scenario(s) not shown — see full report*");
            sb.AppendLine();
        }
    }

    private static void AppendDiagramImages(
        StringBuilder sb,
        IEnumerable<DefaultDiagramsFetcher.DiagramAsCode> diagrams,
        DiagramFormat diagramFormat,
        PlantUmlRendering ciSummaryPlantUmlRendering,
        string plantUmlServerBaseUrl,
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer)
    {
        foreach (var diagram in diagrams)
        {
            if (diagramFormat == DiagramFormat.Mermaid)
            {
                sb.AppendLine("```mermaid");
                sb.AppendLine(diagram.CodeBehind);
                sb.AppendLine("```");
                sb.AppendLine();
            }
            else if (ciSummaryPlantUmlRendering == PlantUmlRendering.Local)
            {
                if (localDiagramRenderer is null)
                    throw new InvalidOperationException(
                        "CiSummaryPlantUmlRendering.Local requires a LocalDiagramRenderer to be configured. " +
                        "Install the TestTrackingDiagrams.PlantUml.Ikvm package and set LocalDiagramRenderer = IkvmPlantUmlRenderer.Render.");

                var imageBytes = localDiagramRenderer(diagram.CodeBehind, PlantUmlImageFormat.Svg);
                var base64 = Convert.ToBase64String(imageBytes);
                sb.AppendLine($"![diagram](data:image/svg+xml;base64,{base64})");
                sb.AppendLine();
            }
            else
            {
                // Server rendering (default, also fallback for BrowserJs)
                if (!string.IsNullOrEmpty(diagram.ImgSrc))
                {
                    sb.AppendLine($"![diagram]({diagram.ImgSrc})");
                }
                else
                {
                    var encoded = PlantUmlTextEncoder.Encode(diagram.CodeBehind);
                    sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{encoded})");
                }
                sb.AppendLine();
            }
        }
    }

    private static string EscapeMarkdown(string text) => text.Replace("|", "\\|");

    private static string EscapeHtml(string text) => text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");

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
