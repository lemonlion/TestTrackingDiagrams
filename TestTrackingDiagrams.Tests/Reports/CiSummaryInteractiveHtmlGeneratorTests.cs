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

    [Fact]
    public void GenerateHtml_contains_IntersectionObserver()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("IntersectionObserver", html);
    }

    [Fact]
    public void GenerateHtml_contains_plantuml_diagram_css_class()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("plantuml-diagram", html);
    }

    [Fact]
    public void GenerateHtml_uses_official_teavm_cdn()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("plantuml.github.io/plantuml/js-plantuml/plantuml.js", html);
        Assert.Contains("plantuml.github.io/plantuml/js-plantuml/viz-global.js", html);
        Assert.DoesNotContain("nickes/plantuml-js", html);
    }

    [Fact]
    public void GenerateHtml_html_encodes_special_characters_in_data_source()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { new DefaultDiagramsFetcher.DiagramAsCode("id-1", "", "@startuml\nAlice -> Bob: <script>alert(\"xss\")</script> & stuff\n@enduml") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&quot;xss&quot;", html);
        Assert.Contains("&amp; stuff", html);
    }

    [Fact]
    public void GenerateHtml_html_encodes_error_message_and_stack_trace()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", error: "Expected <div> & \"value\"", stack: "at <Module>::Run()", id: "f1")) };
        var diagrams = new[] { Diagram("f1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("&lt;div&gt;", html);
        Assert.Contains("&amp; &quot;value&quot;", html);
        Assert.Contains("&lt;Module&gt;", html);
    }

    [Fact]
    public void GenerateHtml_sequential_ids_across_multiple_failed_scenarios()
    {
        var features = new[]
        {
            MakeFeature("F",
                Failed("Bad1", id: "f1"),
                Failed("Bad2", id: "f2"))
        };
        var diagrams = new[] { Diagram("f1"), Diagram("f2") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("id=\"puml-0\"", html);
        Assert.Contains("id=\"puml-1\"", html);
    }

    [Fact]
    public void GenerateHtml_no_diagrams_does_not_emit_diagram_divs()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = Array.Empty<DefaultDiagramsFetcher.DiagramAsCode>();

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.DoesNotContain("id=\"puml-", html);
        // data-source and plantuml-diagram appear in the head script selector,
        // but no actual diagram divs should be emitted
        var diagramDivCount = html.Split("id=\"puml-").Length - 1;
        Assert.Equal(0, diagramDivCount);
    }

    [Fact]
    public void GenerateHtml_failed_scenario_with_no_matching_diagram_does_not_emit_diagram_div()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", id: "f1")) };
        var diagrams = new[] { Diagram("unrelated-id") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("Bad", html);
        Assert.DoesNotContain("id=\"puml-", html);
    }

    [Fact]
    public void GenerateHtml_failed_scenario_null_stack_trace_omits_details_block()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", stack: null, id: "f1")) };
        var diagrams = new[] { Diagram("f1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("Bad", html);
        Assert.DoesNotContain("Stack Trace", html);
    }

    [Fact]
    public void GenerateHtml_failed_scenario_empty_error_message_omits_error_paragraph()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", error: "", id: "f1")) };
        var diagrams = new[] { Diagram("f1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.DoesNotContain("class=\"error\"", html);
    }

    [Fact]
    public void GenerateHtml_failed_scenario_includes_stack_trace_in_details()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", stack: "at MyTest.cs:line 42", id: "f1")) };
        var diagrams = new[] { Diagram("f1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("Stack Trace", html);
        Assert.Contains("at MyTest.cs:line 42", html);
    }

    [Fact]
    public void GenerateHtml_multiple_diagrams_per_single_failed_scenario()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", id: "f1")) };
        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("f1", "", "@startuml\nA->B\n@enduml"),
            new DefaultDiagramsFetcher.DiagramAsCode("f1", "", "@startuml\nC->D\n@enduml"),
            new DefaultDiagramsFetcher.DiagramAsCode("f1", "", "@startuml\nE->F\n@enduml")
        };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        var diagramDivCount = html.Split("id=\"puml-").Length - 1;
        Assert.Equal(3, diagramDivCount);
    }

    [Fact]
    public void GenerateHtml_all_passed_shows_passed_status()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("&#x2705; Passed", html);
        Assert.DoesNotContain("&#x274C; Failed", html);
    }

    [Fact]
    public void GenerateHtml_with_failure_shows_failed_status()
    {
        var features = new[] { MakeFeature("F", Failed("Bad", id: "f1")) };
        var diagrams = new[] { Diagram("f1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("&#x274C; Failed", html);
    }

    [Fact]
    public void GenerateHtml_displays_duration()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        // Start=10:00:00, End=10:02:34 → 2m 34s
        Assert.Contains("2m 34s", html);
    }

    [Fact]
    public void GenerateHtml_displays_scenario_counts()
    {
        var features = new[]
        {
            MakeFeature("F",
                Passed("P1"),
                Failed("F1"),
                new Scenario { Id = Guid.NewGuid().ToString(), DisplayName = "Skipped1", Result = ScenarioResult.Skipped })
        };
        var diagrams = Array.Empty<DefaultDiagramsFetcher.DiagramAsCode>();

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("<td>3</td>", html); // total
        Assert.Contains("<td>1</td>", html); // at least one count of 1
    }

    [Fact]
    public void GenerateHtml_contains_rootMargin_200px()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("rootMargin: '200px'", html);
    }

    [Fact]
    public void GenerateHtml_contains_rendered_guard_and_unobserve()
    {
        var features = new[] { MakeFeature("F", Passed("S", "id-1")) };
        var diagrams = new[] { Diagram("id-1") };

        var html = CiSummaryInteractiveHtmlGenerator.GenerateHtml(features, diagrams, Start, End);

        Assert.Contains("dataset.rendered", html);
        Assert.Contains("observer.unobserve(el)", html);
    }
}
