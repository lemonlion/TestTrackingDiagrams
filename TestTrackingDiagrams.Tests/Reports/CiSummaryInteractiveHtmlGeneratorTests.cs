using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiSummaryInteractiveHtmlGeneratorTests
{
    private static readonly DateTime Start = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End = new(2026, 4, 1, 10, 2, 34, DateTimeKind.Utc);

    private static Feature MakeFeature(string name, params Scenario[] scenarios) =>
        new() { DisplayName = name, Scenarios = scenarios };

    private static Scenario Passed(string name, string id = "") =>
        new() { Id = id == "" ? Guid.NewGuid().ToString() : id, DisplayName = name, Result = ScenarioResult.Passed };

    private static Scenario Failed(string name, string error = "Assertion failed", string? stack = "at Test.cs:line 1", string id = "") =>
        new() { Id = id == "" ? Guid.NewGuid().ToString() : id, DisplayName = name, Result = ScenarioResult.Failed, ErrorMessage = error, ErrorStackTrace = stack };

    private static DefaultDiagramsFetcher.DiagramAsCode Diagram(string testId) =>
        new(testId, "https://plantuml.com/plantuml/png/abc", "@startuml\nA -> B: request\nB --> A: response\n@enduml");

    [Fact]
    public void GenerateHtml_contains_plantuml_js_script_tags()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("plantuml.js", html);
        Assert.Contains("viz-global.js", html);
    }

    [Fact]
    public void GenerateHtml_embeds_raw_plantuml_in_data_attributes()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("data-source", html);
        Assert.Contains("@startuml", html);
    }

    [Fact]
    public void GenerateHtml_contains_render_script()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("plantuml.render", html);
    }

    [Fact]
    public void GenerateHtml_all_passed_includes_first_N_diagrams()
    {
        var scenarios = Enumerable.Range(1, 5).Select(i =>
        {
            var id = $"test-{i}";
            return (scenario: Passed($"Scenario {i}", id), diagram: Diagram(id));
        }).ToArray();

        var features = new[] { MakeFeature("Feature", scenarios.Select(s => s.scenario).ToArray()) };
        var diagrams = scenarios.Select(s => s.diagram).ToArray();

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End, maxDiagrams: 3);

        Assert.Contains("Scenario 1", html);
        Assert.Contains("Scenario 2", html);
        Assert.Contains("Scenario 3", html);
        Assert.DoesNotContain("Scenario 4", html);
    }

    [Fact]
    public void GenerateHtml_with_failures_includes_only_failed_diagrams()
    {
        var passedId = "passed-1";
        var failedId = "failed-1";
        var features = new[]
        {
            MakeFeature("Orders", Passed("Good", passedId), Failed("Bad", id: failedId))
        };
        var diagrams = new[] { Diagram(passedId), Diagram(failedId) };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        // The failed scenario's diagram should be embedded
        Assert.Contains("Bad", html);
        // Count diagram div occurrences — should only have 1 (for the failed scenario)
        var diagramDivCount = html.Split("id=\"puml-").Length - 1;
        Assert.Equal(1, diagramDivCount);
    }

    [Fact]
    public void GenerateHtml_respects_maxDiagrams_limit()
    {
        var scenarios = Enumerable.Range(1, 5).Select(i =>
        {
            var id = $"test-{i}";
            return (scenario: Passed($"Scenario {i}", id), diagram: Diagram(id));
        }).ToArray();

        var features = new[] { MakeFeature("Feature", scenarios.Select(s => s.scenario).ToArray()) };
        var diagrams = scenarios.Select(s => s.diagram).ToArray();

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End, maxDiagrams: 2);

        var diagramDivCount = html.Split("id=\"puml-").Length - 1;
        Assert.Equal(2, diagramDivCount);
    }
}
