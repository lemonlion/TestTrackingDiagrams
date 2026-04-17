using System.Text.RegularExpressions;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ParameterizedGroupRenderTests
{
    private static Feature[] MakeFeature(params Scenario[] scenarios) =>
    [
        new Feature
        {
            DisplayName = "Test Feature",
            Scenarios = scenarios
        }
    ];

    private static Scenario MakeScenario(string id, string displayName, ExecutionResult result = ExecutionResult.Passed,
        string? outlineId = null, Dictionary<string, string>? exampleValues = null,
        string? exampleDisplayName = null, string? errorMessage = null, string? errorStackTrace = null,
        TimeSpan? duration = null, ScenarioStep[]? steps = null) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Result = result,
            OutlineId = outlineId,
            ExampleValues = exampleValues,
            ExampleDisplayName = exampleDisplayName,
            ErrorMessage = errorMessage,
            ErrorStackTrace = errorStackTrace,
            Duration = duration,
            Steps = steps
        };

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakeDiagrams(params (string testId, string plantuml)[] diagrams) =>
        diagrams.Select(d => new DefaultDiagramsFetcher.DiagramAsCode(d.testId, "", d.plantuml)).ToArray();

    private string GenerateReport(Feature[] features, DefaultDiagramsFetcher.DiagramAsCode[]? diagrams = null, string? fileName = null,
        bool groupParameterizedTests = true, int maxParameterColumns = 10)
    {
        diagrams ??= features.SelectMany(f => f.Scenarios).Select(s => new DefaultDiagramsFetcher.DiagramAsCode(s.Id, "", "")).ToArray();
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName ?? $"ParamGroup_{Guid.NewGuid():N}.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs,
            groupParameterizedTests: groupParameterizedTests,
            maxParameterColumns: maxParameterColumns);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Grouped_scenarios_render_param_test_table()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Process(region: UK)", outlineId: "Process", exampleValues: new() { ["region"] = "UK" }),
            MakeScenario("s2", "Process(region: US)", outlineId: "Process", exampleValues: new() { ["region"] = "US" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("param-test-table", content);
        Assert.Contains("scenario-parameterized", content);
    }

    [Fact]
    public void R1_scalar_columns_shows_input_parameters_header()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Calc(a: 1, b: 2)", outlineId: "Calc",
                exampleValues: new() { ["a"] = "1", ["b"] = "2" }),
            MakeScenario("s2", "Calc(a: 3, b: 4)", outlineId: "Calc",
                exampleValues: new() { ["a"] = "3", ["b"] = "4" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("Input Parameters", content);
        Assert.Contains("sub-header", content);
    }

    [Fact]
    public void R1_renders_parameter_name_columns()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Calc(x: 1, y: 2)", outlineId: "Calc",
                exampleValues: new() { ["x"] = "1", ["y"] = "2" }),
            MakeScenario("s2", "Calc(x: 3, y: 4)", outlineId: "Calc",
                exampleValues: new() { ["x"] = "3", ["y"] = "4" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains(">x</th>", content);
        Assert.Contains(">y</th>", content);
    }

    [Fact]
    public void R1_renders_parameter_values_in_cells()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Calc(x: 1)", outlineId: "Calc", exampleValues: new() { ["x"] = "1" }),
            MakeScenario("s2", "Calc(x: 42)", outlineId: "Calc", exampleValues: new() { ["x"] = "42" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains(">1</td>", content);
        Assert.Contains(">42</td>", content);
    }

    [Fact]
    public void R0_fallback_shows_test_case_column()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Happy path UK", outlineId: "Scenarios",
                exampleDisplayName: "Happy path UK"),
            MakeScenario("s2", "Happy path US", outlineId: "Scenarios",
                exampleDisplayName: "Happy path US")
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("Test Case", content);
        Assert.Contains("Happy path UK", content);
        Assert.Contains("Happy path US", content);
    }

    [Fact]
    public void Grouped_scenarios_show_status_badges()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test",
                exampleValues: new() { ["a"] = "1" }, result: ExecutionResult.Passed),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test",
                exampleValues: new() { ["a"] = "2" }, result: ExecutionResult.Failed,
                errorMessage: "Expected true")
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("badge-pass", content);
        Assert.Contains("badge-fail", content);
    }

    [Fact]
    public void Row_click_calls_selectRow_with_prefix()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("selectRow(this,", content);
        Assert.Contains("data-row-idx=\"0\"", content);
        Assert.Contains("data-row-idx=\"1\"", content);
    }

    [Fact]
    public void First_row_has_active_class()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("row-active", content);
    }

    [Fact]
    public void Failed_scenario_renders_failure_detail_in_panel()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test",
                exampleValues: new() { ["a"] = "1" }, result: ExecutionResult.Passed),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test",
                exampleValues: new() { ["a"] = "2" }, result: ExecutionResult.Failed,
                errorMessage: "Expected true but got false",
                errorStackTrace: "at Test.cs:42")
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("param-detail-panel", content);
        Assert.Contains("failure-result", content);
        Assert.Contains("Expected true but got false", content);
    }

    [Fact]
    public void Steps_render_in_detail_panel()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test",
                exampleValues: new() { ["a"] = "1" },
                steps: [new ScenarioStep { Text = "Given a setup", Keyword = "Given" }]),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test",
                exampleValues: new() { ["a"] = "2" },
                steps: [new ScenarioStep { Text = "Given a setup", Keyword = "Given" }])
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("param-detail-panel", content);
        Assert.Contains("scenario-steps", content);
    }

    [Fact]
    public void Identical_diagrams_show_badge()
    {
        var puml = "@startuml\nA -> B: hello\n@enduml";
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var diagrams = MakeDiagrams(("s1", puml), ("s2", puml));
        var content = GenerateReport(MakeFeature(scenarios), diagrams);

        Assert.Contains("param-diagram-identical-badge", content);
        Assert.Contains("All diagrams identical", content);
    }

    [Fact]
    public void Different_diagrams_render_per_row_switching()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var diagrams = MakeDiagrams(
            ("s1", "@startuml\nA -> B: msg1\n@enduml"),
            ("s2", "@startuml\nA -> B: msg2\n@enduml"));
        var content = GenerateReport(MakeFeature(scenarios), diagrams);

        Assert.DoesNotContain("class=\"param-diagram-identical-badge\"", content);
        Assert.Contains("class=\"example-diagrams\"", content);
        // Two diagram divs with different IDs
        var diagramDivs = Regex.Matches(content, @"id=""pgrp\d+-diagram-\d+""");
        Assert.True(diagramDivs.Count >= 2);
    }

    [Fact]
    public void Grouping_disabled_renders_individual_scenarios_for_non_outline_groups()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)"),
            MakeScenario("s2", "Test(a: 2)")
        };
        var content = GenerateReport(MakeFeature(scenarios), groupParameterizedTests: false);

        // Should NOT have param-test-table since these have no OutlineId and grouping is disabled
        Assert.DoesNotContain("<table class=\"param-test-table\"", content);
    }

    [Fact]
    public void Grouping_disabled_still_groups_OutlineId_scenarios()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Outline(a: 1)", outlineId: "Outline", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Outline(a: 2)", outlineId: "Outline", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios), groupParameterizedTests: false);

        // OutlineId groups should still render as grouped
        Assert.Contains("param-test-table", content);
    }

    [Fact]
    public void Ungrouped_scenario_renders_individually()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Create order", result: ExecutionResult.Passed),
            MakeScenario("s2", "Delete order", result: ExecutionResult.Passed)
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // These should be individual scenario sections, not grouped
        Assert.DoesNotContain("<table class=\"param-test-table\"", content);
        Assert.Contains("Create order", content);
        Assert.Contains("Delete order", content);
    }

    [Fact]
    public void Group_summary_shows_pass_fail_counts()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test",
                exampleValues: new() { ["a"] = "1" }, result: ExecutionResult.Passed),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test",
                exampleValues: new() { ["a"] = "2" }, result: ExecutionResult.Failed,
                errorMessage: "fail"),
            MakeScenario("s3", "Test(a: 3)", outlineId: "Test",
                exampleValues: new() { ["a"] = "3" }, result: ExecutionResult.Passed)
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("1 failed", content);
        Assert.Contains("2/3 passed", content);
    }

    [Fact]
    public void Group_has_aggregate_duration()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test",
                exampleValues: new() { ["a"] = "1" }, duration: TimeSpan.FromMilliseconds(500)),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test",
                exampleValues: new() { ["a"] = "2" }, duration: TimeSpan.FromMilliseconds(300))
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("duration-badge", content);
        Assert.Contains("data-duration-ms=", content);
    }

    [Fact]
    public void SelectRow_javascript_function_is_present()
    {
        var scenarios = new[] { MakeScenario("s1", "Test", result: ExecutionResult.Passed) };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("function selectRow(", content);
    }

    [Fact]
    public void Display_name_based_grouping_produces_param_table()
    {
        // Two scenarios with same method prefix but no OutlineId - should be grouped via display name parsing
        var scenarios = new[]
        {
            MakeScenario("s1", "Calculate(x: 10, y: 20)"),
            MakeScenario("s2", "Calculate(x: 30, y: 40)")
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("param-test-table", content);
        Assert.Contains("scenario-parameterized", content);
        Assert.Contains(">x</th>", content);
        Assert.Contains(">y</th>", content);
    }

    [Fact]
    public void Skipped_row_has_correct_status_badge()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test",
                exampleValues: new() { ["a"] = "1" }, result: ExecutionResult.Passed),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test",
                exampleValues: new() { ["a"] = "2" }, result: ExecutionResult.Skipped)
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("badge-skip", content);
        Assert.Contains("row-skipped", content);
    }

    [Fact]
    public void MaxParameterColumns_exceeded_falls_back_to_R0()
    {
        var exampleValues = new Dictionary<string, string>();
        for (var i = 0; i < 5; i++)
            exampleValues[$"p{i}"] = $"v{i}";

        var scenarios = new[]
        {
            MakeScenario("s1", "Big(p0: v0, p1: v1, p2: v2, p3: v3, p4: v4)",
                outlineId: "Big", exampleValues: new(exampleValues)),
            MakeScenario("s2", "Big(p0: x0, p1: x1, p2: x2, p3: x3, p4: x4)",
                outlineId: "Big", exampleValues: new(exampleValues.ToDictionary(kv => kv.Key, kv => "x" + kv.Value[1..])))
        };

        // Set maxParameterColumns=2 to force fallback
        var content = GenerateReport(MakeFeature(scenarios), maxParameterColumns: 2);

        Assert.Contains("Test Case", content);
        Assert.DoesNotContain("Input Parameters", content);
    }

    [Fact]
    public void HTML_encodes_parameter_values()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(x: <script>)", outlineId: "Test",
                exampleValues: new() { ["x"] = "<script>alert(1)</script>" }),
            MakeScenario("s2", "Test(x: safe)", outlineId: "Test",
                exampleValues: new() { ["x"] = "safe" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.DoesNotContain("<script>alert(1)</script>", content);
        Assert.Contains("&lt;script&gt;", content);
    }

    // ── Copy scenario name button on groups ──

    [Fact]
    public void Group_summary_has_copy_scenario_name_button()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Proc(x: 1)", outlineId: "Proc", exampleValues: new() { ["x"] = "1" }),
            MakeScenario("s2", "Proc(x: 2)", outlineId: "Proc", exampleValues: new() { ["x"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("copy-scenario-name", content);
        Assert.Contains("data-scenario-name=\"Proc\"", content);
    }

    [Fact]
    public void Group_summary_has_scenario_link()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Proc(x: 1)", outlineId: "Proc", exampleValues: new() { ["x"] = "1" }),
            MakeScenario("s2", "Proc(x: 2)", outlineId: "Proc", exampleValues: new() { ["x"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("class=\"scenario-link\"", content);
        Assert.Contains("href=\"#scenario-proc\"", content);
    }

    // ── Deep link per row ──

    [Fact]
    public void Rows_have_data_scenario_id_for_deep_linking()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // Each row should have data-scenario-id attribute in table HTML
        var dataScenarioIds = Regex.Matches(content, @"<tr[^>]+data-scenario-id=""([^""]+)""");
        Assert.Equal(2, dataScenarioIds.Count);
    }

    [Fact]
    public void Deep_link_JS_handles_row_inside_parameterized_group()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // The JS should have code for finding rows by data-scenario-id
        Assert.Contains("data-scenario-id", content);
        Assert.Contains("scenario-parameterized", content);
    }

    // ── Search row highlighting ──

    [Fact]
    public void Rows_have_data_row_search_for_search_matching()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: foo)", outlineId: "Test", exampleValues: new() { ["a"] = "foo" }),
            MakeScenario("s2", "Test(a: bar)", outlineId: "Test", exampleValues: new() { ["a"] = "bar" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        var rowSearchAttrs = Regex.Matches(content, @"data-row-search=""[^""]+""");
        Assert.Equal(2, rowSearchAttrs.Count);
        // Each row's search text includes its display name
        Assert.Contains("test(a: foo)", content.ToLowerInvariant());
        Assert.Contains("test(a: bar)", content.ToLowerInvariant());
    }

    [Fact]
    public void Search_JS_includes_row_search_match_logic()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("row-search-match", content);
        Assert.Contains("data-row-search", content);
    }

    // ── SelectRow JS switching for activity/flame ──

    [Fact]
    public void SelectRow_JS_switches_activity_and_flame_divs()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // The selectRow JS should handle activity and flame divs
        Assert.Contains("-activity-", content);
        Assert.Contains("-flame-", content);
    }
}
