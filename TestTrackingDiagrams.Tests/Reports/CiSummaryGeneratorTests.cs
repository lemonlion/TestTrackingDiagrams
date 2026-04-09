using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiSummaryGeneratorTests
{
    private static readonly DateTime Start = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 4, 1, 10, 2, 34, DateTimeKind.Utc);

    private static Feature MakeFeature(string name, params Scenario[] scenarios) =>
        new() { DisplayName = name, Scenarios = scenarios };

    private static Scenario Passed(string name, string id = "") =>
        new() { Id = id == "" ? Guid.NewGuid().ToString() : id, DisplayName = name, Result = ScenarioResult.Passed };

    private static Scenario Failed(string name, string error = "Assertion failed", string? stack = "at Test.cs:line 1", string id = "") =>
        new() { Id = id == "" ? Guid.NewGuid().ToString() : id, DisplayName = name, Result = ScenarioResult.Failed, ErrorMessage = error, ErrorStackTrace = stack };

    private static Scenario Skipped(string name) =>
        new() { Id = Guid.NewGuid().ToString(), DisplayName = name, Result = ScenarioResult.Skipped };

    private static DefaultDiagramsFetcher.DiagramAsCode Diagram(string testId, string imgSrc = "https://plantuml.com/plantuml/png/abc123") =>
        new(testId, imgSrc, "@startuml\nA -> B\n@enduml");

    [Fact]
    public void GenerateMarkdown_all_passed_shows_passed_status()
    {
        var features = new[] { MakeFeature("Orders", Passed("Create order")) };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.Contains("✅ Passed", markdown);
        Assert.DoesNotContain("❌", markdown);
    }

    [Fact]
    public void GenerateMarkdown_all_passed_includes_diagram_images_for_first_N_scenarios()
    {
        var id1 = "test-1";
        var id2 = "test-2";
        var features = new[]
        {
            MakeFeature("Orders", Passed("Create order", id1), Passed("Delete order", id2))
        };
        var diagrams = new[] { Diagram(id1, "https://plantuml.com/img1"), Diagram(id2, "https://plantuml.com/img2") };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End);

        Assert.Contains("![", markdown);
        Assert.Contains("https://plantuml.com/img1", markdown);
        Assert.Contains("https://plantuml.com/img2", markdown);
    }

    [Fact]
    public void GenerateMarkdown_all_passed_respects_maxDiagrams_limit()
    {
        var scenarios = Enumerable.Range(1, 5).Select(i =>
        {
            var id = $"test-{i}";
            return (scenario: Passed($"Scenario {i}", id), diagram: Diagram(id, $"https://plantuml.com/img{i}"));
        }).ToArray();

        var features = new[] { MakeFeature("Feature", scenarios.Select(s => s.scenario).ToArray()) };
        var diagrams = scenarios.Select(s => s.diagram).ToArray();

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End, maxDiagrams: 2);

        Assert.Contains("https://plantuml.com/img1", markdown);
        Assert.Contains("https://plantuml.com/img2", markdown);
        Assert.DoesNotContain("https://plantuml.com/img3", markdown);
    }

    [Fact]
    public void GenerateMarkdown_all_passed_shows_truncation_notice_when_more_scenarios_than_max()
    {
        var scenarios = Enumerable.Range(1, 5).Select(i =>
        {
            var id = $"test-{i}";
            return (scenario: Passed($"Scenario {i}", id), diagram: Diagram(id));
        }).ToArray();

        var features = new[] { MakeFeature("Feature", scenarios.Select(s => s.scenario).ToArray()) };
        var diagrams = scenarios.Select(s => s.diagram).ToArray();

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End, maxDiagrams: 2);

        Assert.Contains("3 more", markdown);
        Assert.Contains("full report", markdown.ToLower());
    }

    [Fact]
    public void GenerateMarkdown_with_failures_shows_failed_status()
    {
        var features = new[] { MakeFeature("Orders", Failed("Bad order")) };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.Contains("❌ Failed", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_failures_shows_only_failed_scenario_diagrams()
    {
        var passedId = "passed-1";
        var failedId = "failed-1";
        var features = new[]
        {
            MakeFeature("Orders", Passed("Good order", passedId), Failed("Bad order", id: failedId))
        };
        var diagrams = new[] { Diagram(passedId, "https://plantuml.com/passed"), Diagram(failedId, "https://plantuml.com/failed") };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End);

        Assert.Contains("https://plantuml.com/failed", markdown);
        Assert.DoesNotContain("https://plantuml.com/passed", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_failures_includes_error_message_and_stack_trace()
    {
        var features = new[] { MakeFeature("Orders", Failed("Bad order", "Expected 200 but got 500", "at OrderTests.cs:line 42")) };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.Contains("Expected 200 but got 500", markdown);
        Assert.Contains("at OrderTests.cs:line 42", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_failures_respects_maxDiagrams_cap()
    {
        var scenarios = Enumerable.Range(1, 5).Select(i =>
        {
            var id = $"fail-{i}";
            return (scenario: Failed($"Failure {i}", id: id), diagram: Diagram(id, $"https://plantuml.com/fail{i}"));
        }).ToArray();

        var features = new[] { MakeFeature("Feature", scenarios.Select(s => s.scenario).ToArray()) };
        var diagrams = scenarios.Select(s => s.diagram).ToArray();

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End, maxDiagrams: 2);

        Assert.Contains("https://plantuml.com/fail1", markdown);
        Assert.Contains("https://plantuml.com/fail2", markdown);
        Assert.DoesNotContain("https://plantuml.com/fail3", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_failures_does_not_include_passing_diagrams()
    {
        var passedId = "passed-1";
        var failedId = "failed-1";
        var features = new[]
        {
            MakeFeature("Orders", Passed("Good", passedId), Failed("Bad", id: failedId))
        };
        var diagrams = new[] { Diagram(passedId, "https://plantuml.com/passed-img"), Diagram(failedId, "https://plantuml.com/failed-img") };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End);

        Assert.DoesNotContain("https://plantuml.com/passed-img", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_failures_shows_truncation_notice_when_more_failures_than_max()
    {
        var scenarios = Enumerable.Range(1, 5).Select(i =>
        {
            var id = $"fail-{i}";
            return (scenario: Failed($"Failure {i}", id: id), diagram: Diagram(id));
        }).ToArray();

        var features = new[] { MakeFeature("Feature", scenarios.Select(s => s.scenario).ToArray()) };
        var diagrams = scenarios.Select(s => s.diagram).ToArray();

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, diagrams, Start, End, maxDiagrams: 2);

        Assert.Contains("3 more", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_no_features_returns_minimal_summary()
    {
        var markdown = CiSummaryGenerator.GenerateMarkdown([], [], Start, End);

        Assert.Contains("# Diagrammed Test Run Summary", markdown);
        Assert.Contains("0", markdown);
    }

    [Fact]
    public void GenerateMarkdown_formats_duration_seconds_correctly()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 0, 0, 45, DateTimeKind.Utc);
        var features = new[] { MakeFeature("F", Passed("S")) };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], start, end);

        Assert.Contains("45s", markdown);
    }

    [Fact]
    public void GenerateMarkdown_formats_duration_minutes_correctly()
    {
        var markdown = CiSummaryGenerator.GenerateMarkdown(new[] { MakeFeature("F", Passed("S")) }, [], Start, End);

        Assert.Contains("2m 34s", markdown);
    }

    [Fact]
    public void GenerateMarkdown_escapes_pipe_characters_in_scenario_names()
    {
        var features = new[] { MakeFeature("Orders", Passed("Scenario with | pipe")) };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        // Pipe characters must be escaped in Markdown tables
        Assert.DoesNotContain("| pipe |", markdown);
    }

    [Fact]
    public void GenerateMarkdown_scenario_with_no_matching_diagram_omits_image()
    {
        var features = new[] { MakeFeature("Orders", Passed("No diagram scenario", "orphan-id")) };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.DoesNotContain("![", markdown);
    }

    [Fact]
    public void GenerateMarkdown_with_skipped_shows_correct_counts()
    {
        var features = new[]
        {
            MakeFeature("Orders", Passed("Good"), Skipped("WIP"), Failed("Bad"))
        };

        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.Contains("1", markdown); // passed
        Assert.Contains("1", markdown); // failed
        Assert.Contains("1", markdown); // skipped
        Assert.Contains("3", markdown); // total
    }

    [Fact]
    public void TruncateNotes_returns_original_when_notes_are_short()
    {
        var plantUml = "@startuml\nnote left\nline1\nline2\nend note\n@enduml\n";
        var result = CiSummaryGenerator.TruncateNotes(plantUml);
        Assert.Equal(plantUml, result);
    }

    [Fact]
    public void TruncateNotes_truncates_notes_exceeding_10_lines()
    {
        var noteLines = string.Join("\n", Enumerable.Range(1, 15).Select(i => $"line{i}"));
        var plantUml = $"@startuml\nnote left\n{noteLines}\nend note\n@enduml\n";
        var result = CiSummaryGenerator.TruncateNotes(plantUml);

        Assert.Contains("line10", result);
        Assert.Contains("...", result);
        Assert.DoesNotContain("line11", result);
        Assert.Contains("end note", result);
        Assert.Contains("@startuml", result);
        Assert.Contains("@enduml", result);
    }

    [Fact]
    public void TruncateNotes_preserves_notes_with_exactly_10_lines()
    {
        var noteLines = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"line{i}"));
        var plantUml = $"@startuml\nnote left\n{noteLines}\nend note\n@enduml\n";
        var result = CiSummaryGenerator.TruncateNotes(plantUml);
        Assert.Equal(plantUml, result);
    }

    [Fact]
    public void TruncateNotes_handles_multiple_notes_only_truncates_long_ones()
    {
        var shortNote = "line1\nline2";
        var longNote = string.Join("\n", Enumerable.Range(1, 15).Select(i => $"long{i}"));
        var plantUml = $"@startuml\nnote left\n{shortNote}\nend note\nnote right\n{longNote}\nend note\n@enduml\n";
        var result = CiSummaryGenerator.TruncateNotes(plantUml);

        Assert.Contains("line1", result);
        Assert.Contains("line2", result);
        Assert.Contains("long10", result);
        Assert.Contains("...", result);
        Assert.DoesNotContain("long11", result);
    }

    [Fact]
    public void GenerateMarkdown_failed_scenario_uses_darkred_styling()
    {
        var features = new[] { MakeFeature("Orders", Failed("Bad order")) };
        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.Contains("color: darkred", markdown);
    }

    [Fact]
    public void GenerateMarkdown_failed_scenario_stack_trace_is_open_by_default()
    {
        var features = new[] { MakeFeature("Orders", Failed("Bad order", "err", "at Test.cs:1")) };
        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        Assert.Contains("<details open><summary>Stack Trace</summary>", markdown);
    }

    [Fact]
    public void GenerateMarkdown_failed_scenario_is_collapsed_by_default()
    {
        var features = new[] { MakeFeature("Orders", Failed("Bad order")) };
        var markdown = CiSummaryGenerator.GenerateMarkdown(features, [], Start, End);

        // Should NOT have <details open> for the outer scenario
        Assert.DoesNotContain("<details open><summary><strong", markdown);
        Assert.Contains("<details><summary><strong", markdown);
    }
}
