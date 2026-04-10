using System.Text;
using System.Text.RegularExpressions;
using TestTrackingDiagrams.PlantUml;

namespace TestTrackingDiagrams.Reports;

public static partial class CiSummaryGenerator
{
    private const int NoteTruncateLines = 10;

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

        var diagramsByTestId = diagrams.ToLookup(d => d.TestRuntimeId);

        if (hasFailed)
            AppendFailedScenarios(sb, features, diagramsByTestId, failed, maxDiagrams, diagramFormat, plantUmlServerBaseUrl);
        else
            AppendPassedDiagrams(sb, features, diagramsByTestId, total, maxDiagrams, diagramFormat, plantUmlServerBaseUrl);

        return sb.ToString();
    }

    private static void AppendFailedScenarios(
        StringBuilder sb,
        Feature[] features,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> diagramsByTestId,
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
            var failedScenarios = feature.Scenarios.Where(s => s.Result == ScenarioResult.Failed);
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

                AppendDiagramImages(sb, diagramsByTestId[scenario.Id], diagramFormat, plantUmlServerBaseUrl);

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
        string plantUmlServerBaseUrl)
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
            AppendDiagramImages(sb, diagramsByTestId[scenario.Id], diagramFormat, plantUmlServerBaseUrl);

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
        string plantUmlServerBaseUrl)
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
            else
            {
                // GitHub's HTML sanitizer strips <svg> elements and data: URIs from job summaries.
                // PlantUml server URLs are the only image approach GitHub allows.
                var preparedPlantUml = DeactivateUrls(diagram.CodeBehind);
                var truncatedPlantUml = TruncateNotes(preparedPlantUml);
                var wasTruncated = truncatedPlantUml != preparedPlantUml;

                if (wasTruncated)
                {
                    var truncatedEncoded = PlantUmlTextEncoder.Encode(truncatedPlantUml);
                    sb.AppendLine("<details open><summary>Truncated Sequence Diagram</summary>");
                    sb.AppendLine();
                    sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{truncatedEncoded})");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                    sb.AppendLine();
                }

                var fullEncoded = PlantUmlTextEncoder.Encode(preparedPlantUml);
                if (wasTruncated)
                {
                    sb.AppendLine("<details><summary>Full Sequence Diagram</summary>");
                    sb.AppendLine();
                    sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{fullEncoded})");
                    sb.AppendLine();
                    sb.AppendLine("</details>");
                }
                else
                {
                    sb.AppendLine($"![diagram]({plantUmlServerBaseUrl}/svg/{fullEncoded})");
                }
                sb.AppendLine();
            }
        }
    }

    internal static string TruncateNotes(string plantUml)
    {
        var lines = plantUml.Split('\n');
        var result = new StringBuilder();
        var inNote = false;
        var noteLines = new List<string>();
        var anyTruncated = false;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (!inNote && NoteStartRegex().IsMatch(trimmed))
            {
                inNote = true;
                noteLines.Clear();
                result.AppendLine(line.TrimEnd('\r'));
                continue;
            }

            if (inNote)
            {
                if (trimmed.TrimEnd('\r') == "end note")
                {
                    inNote = false;
                    if (noteLines.Count > NoteTruncateLines)
                    {
                        anyTruncated = true;
                        for (var i = 0; i < NoteTruncateLines; i++)
                            result.AppendLine(noteLines[i]);
                        result.AppendLine("...");
                    }
                    else
                    {
                        foreach (var noteLine in noteLines)
                            result.AppendLine(noteLine);
                    }
                    result.AppendLine("end note");
                }
                else
                {
                    noteLines.Add(line.TrimEnd('\r'));
                }
                continue;
            }

            result.AppendLine(line.TrimEnd('\r'));
        }

        return anyTruncated ? result.ToString().TrimEnd() + "\n" : plantUml;
    }

    [GeneratedRegex(@"^note\S*\s+(left|right|over)", RegexOptions.IgnoreCase)]
    private static partial Regex NoteStartRegex();

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
