using System.Text;
using System.Text.RegularExpressions;
using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Generates a markdown summary of the test run including pass/fail counts,
/// failure details with diagrams, and execution timing.
/// </summary>
public static partial class CiSummaryGenerator
{

    public static string GenerateMarkdown(
        Feature[] features,
        DefaultDiagramsFetcher.DiagramAsCode[] truncatedDiagrams,
        DefaultDiagramsFetcher.DiagramAsCode[] fullDiagrams,
        DateTime startRunTime,
        DateTime endRunTime,
        int maxDiagrams = 10,
        DiagramFormat diagramFormat = DiagramFormat.PlantUml,
        string plantUmlServerBaseUrl = "https://plantuml.com/plantuml",
        Func<string, PlantUmlImageFormat, byte[]>? localDiagramRenderer = null)
    {
        var sb = new StringBuilder();
        var allScenarios = features.SelectMany(f => f.Scenarios).ToArray();
        var passed = allScenarios.Count(s => s.Result == ExecutionResult.Passed);
        var failed = allScenarios.Count(s => s.Result == ExecutionResult.Failed);
        var skipped = allScenarios.Count(s => s.Result == ExecutionResult.Skipped);
        var total = allScenarios.Length;
        var hasFailed = failed > 0;
        var status = hasFailed ? "❌ Failed" : "✅ Passed";
        var duration = FormatDuration(endRunTime - startRunTime);

        sb.AppendLine("# Diagrammed Test Run Summary");
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

        var truncatedByTestId = truncatedDiagrams.ToLookup(d => d.TestRuntimeId);
        var fullByTestId = fullDiagrams.ToLookup(d => d.TestRuntimeId);

        if (hasFailed)
            AppendFailedScenarios(sb, features, truncatedByTestId, fullByTestId, failed, maxDiagrams, diagramFormat, plantUmlServerBaseUrl);
        else
            AppendPassedDiagrams(sb, features, truncatedByTestId, fullByTestId, total, maxDiagrams, diagramFormat, plantUmlServerBaseUrl);

        return sb.ToString();
    }

    private static void AppendFailedScenarios(
        StringBuilder sb,
        Feature[] features,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> truncatedByTestId,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> fullByTestId,
        int totalFailed,
        int maxDiagrams,
        DiagramFormat diagramFormat,
        string plantUmlServerBaseUrl)
    {
        sb.AppendLine($"## ❌ Failed Scenarios ({totalFailed})");
        sb.AppendLine();

        var shown = 0;
        foreach (var feature in features)
        {
            var failedScenarios = feature.Scenarios.Where(s => s.Result == ExecutionResult.Failed);
            foreach (var scenario in failedScenarios)
            {
                if (shown >= maxDiagrams) break;

                sb.AppendLine($"<details><summary>❌ <strong>{EscapeHtml(feature.DisplayName)} — {EscapeHtml(scenario.DisplayName)}</strong></summary>");
                sb.AppendLine();

                if (!string.IsNullOrEmpty(scenario.ErrorMessage))
                {
                    sb.AppendLine($"**Error:** {EscapeMarkdown(scenario.ErrorMessage)}");
                    sb.AppendLine();
                }

                if (!string.IsNullOrEmpty(scenario.ErrorStackTrace))
                {
                    sb.AppendLine("<details open><summary>Stack Trace</summary>");
                    sb.AppendLine();
                    sb.AppendLine("```");
                    sb.AppendLine(scenario.ErrorStackTrace);
                    sb.AppendLine("```");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                    sb.AppendLine();
                }

                AppendDiagramImages(sb, truncatedByTestId[scenario.Id], fullByTestId[scenario.Id], diagramFormat, plantUmlServerBaseUrl);

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
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> truncatedByTestId,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> fullByTestId,
        int totalScenarios,
        int maxDiagrams,
        DiagramFormat diagramFormat,
        string plantUmlServerBaseUrl)
    {
        var scenariosWithDiagrams = features
            .SelectMany(f => f.Scenarios.Select(s => (Feature: f, Scenario: s)))
            .Where(x => truncatedByTestId[x.Scenario.Id].Any())
            .ToArray();

        if (scenariosWithDiagrams.Length == 0) return;

        sb.AppendLine("## Sequence Diagrams");
        sb.AppendLine();

        var shown = 0;
        foreach (var (feature, scenario) in scenariosWithDiagrams)
        {
            if (shown >= maxDiagrams) break;

            sb.AppendLine($"<details><summary>✅ <strong>{EscapeHtml(feature.DisplayName)} — {EscapeHtml(scenario.DisplayName)}</strong></summary>");
            sb.AppendLine();
            AppendDiagramImages(sb, truncatedByTestId[scenario.Id], fullByTestId[scenario.Id], diagramFormat, plantUmlServerBaseUrl);

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
        IEnumerable<DefaultDiagramsFetcher.DiagramAsCode> truncatedDiagrams,
        IEnumerable<DefaultDiagramsFetcher.DiagramAsCode> fullDiagrams,
        DiagramFormat diagramFormat,
        string plantUmlServerBaseUrl)
    {
        var truncatedList = truncatedDiagrams.ToArray();
        var fullList = fullDiagrams.ToArray();

        // Check if truncation actually changed anything by comparing content
        var wasTruncated = truncatedList.Length != fullList.Length ||
            !truncatedList.Select(d => d.CodeBehind).SequenceEqual(fullList.Select(d => d.CodeBehind));

        if (wasTruncated)
        {
            // Render all truncated diagram parts first
            var isMultiPart = truncatedList.Length > 1;
            for (var i = 0; i < truncatedList.Length; i++)
            {
                var partSuffix = isMultiPart ? $" (Part {i + 1})" : "";
                var encoded = PlantUmlTextEncoder.Encode(DeactivateUrls(truncatedList[i].CodeBehind));
                sb.AppendLine($"<details open><summary>Truncated Sequence Diagram{partSuffix}</summary>");
                sb.AppendLine();
                sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{encoded})");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }

            // Then render all full diagram parts
            isMultiPart = fullList.Length > 1;
            for (var i = 0; i < fullList.Length; i++)
            {
                var partSuffix = isMultiPart ? $" (Part {i + 1})" : "";
                var encoded = PlantUmlTextEncoder.Encode(DeactivateUrls(fullList[i].CodeBehind));
                sb.AppendLine($"<details><summary>Full Sequence Diagram{partSuffix}</summary>");
                sb.AppendLine();
                sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{encoded})");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }

            // PlantUML source for all full diagram parts
            for (var i = 0; i < fullList.Length; i++)
            {
                var partSuffix = isMultiPart ? $" (Part {i + 1})" : "";
                sb.AppendLine($"<details><summary>Full Sequence Diagram{partSuffix} - PlantUML</summary>");
                sb.AppendLine();
                sb.AppendLine("```plantuml");
                sb.AppendLine(fullList[i].CodeBehind);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
        }
        else
        {
            // No truncation — render diagrams directly
            var isMultiPart = truncatedList.Length > 1;
            for (var i = 0; i < truncatedList.Length; i++)
            {
                var partSuffix = isMultiPart ? $" (Part {i + 1})" : "";
                var encoded = PlantUmlTextEncoder.Encode(DeactivateUrls(truncatedList[i].CodeBehind));
                if (isMultiPart)
                {
                    sb.AppendLine($"<details open><summary>Sequence Diagram{partSuffix}</summary>");
                    sb.AppendLine();
                    sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{encoded})");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                }
                else
                {
                    sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{encoded})");
                }
                sb.AppendLine();
            }

            // PlantUML source for all diagram parts
            for (var i = 0; i < truncatedList.Length; i++)
            {
                var partSuffix = isMultiPart ? $" (Part {i + 1})" : "";
                var label = isMultiPart ? $"Sequence Diagram{partSuffix} - PlantUML" : "Sequence Diagram - PlantUML";
                sb.AppendLine($"<details><summary>{label}</summary>");
                sb.AppendLine();
                sb.AppendLine("```plantuml");
                sb.AppendLine(truncatedList[i].CodeBehind);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("</details>");
                sb.AppendLine();
            }
        }
    }

    /// <summary>
    /// Breaks URL patterns in PlantUML source so the server doesn't render them as
    /// SVG hyperlinks (which are non-clickable inside img tags and look broken).
    /// Replaces "://" with "&#58;//" — PlantUML renders &#58; as ":" but doesn't
    /// recognize it as a URL protocol prefix.
    /// </summary>
    internal static string DeactivateUrls(string plantUml) =>
        UrlProtocolRegex().Replace(plantUml, "${proto}&#58;//");

    [GeneratedRegex(@"(?<proto>https?)://")]
    private static partial Regex UrlProtocolRegex();

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
