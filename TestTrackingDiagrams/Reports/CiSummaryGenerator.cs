using System.Text;

namespace TestTrackingDiagrams.Reports;

public static class CiSummaryGenerator
{
    public static string GenerateMarkdown(
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
            AppendFailedScenarios(sb, features, diagramsByTestId, failed, maxDiagrams);
        else
            AppendPassedDiagrams(sb, features, diagramsByTestId, total, maxDiagrams);

        return sb.ToString();
    }

    private static void AppendFailedScenarios(
        StringBuilder sb,
        Feature[] features,
        ILookup<string, DefaultDiagramsFetcher.DiagramAsCode> diagramsByTestId,
        int totalFailed,
        int maxDiagrams)
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

                sb.AppendLine($"### {EscapeMarkdown(feature.DisplayName)} — {EscapeMarkdown(scenario.DisplayName)}");
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

                AppendDiagramImages(sb, diagramsByTestId[scenario.Id]);
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
        int maxDiagrams)
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

            sb.AppendLine($"### {EscapeMarkdown(feature.DisplayName)} — {EscapeMarkdown(scenario.DisplayName)}");
            sb.AppendLine();
            AppendDiagramImages(sb, diagramsByTestId[scenario.Id]);
            shown++;
        }

        var remaining = totalScenarios - shown;
        if (remaining > 0)
        {
            sb.AppendLine($"*{remaining} more scenario(s) not shown — see full report*");
            sb.AppendLine();
        }
    }

    private static void AppendDiagramImages(StringBuilder sb, IEnumerable<DefaultDiagramsFetcher.DiagramAsCode> diagrams)
    {
        foreach (var diagram in diagrams)
        {
            sb.AppendLine($"![diagram]({diagram.ImgSrc})");
            sb.AppendLine();
        }
    }

    private static string EscapeMarkdown(string text) => text.Replace("|", "\\|");

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
