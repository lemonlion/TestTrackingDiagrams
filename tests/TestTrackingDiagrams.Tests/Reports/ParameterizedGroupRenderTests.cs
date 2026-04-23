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
        Dictionary<string, object?>? exampleRawValues = null,
        string? exampleDisplayName = null, string? errorMessage = null, string? errorStackTrace = null,
        TimeSpan? duration = null, ScenarioStep[]? steps = null, bool isHappyPath = false) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Result = result,
            OutlineId = outlineId,
            ExampleValues = exampleValues,
            ExampleRawValues = exampleRawValues,
            ExampleDisplayName = exampleDisplayName,
            ErrorMessage = errorMessage,
            ErrorStackTrace = errorStackTrace,
            Duration = duration,
            Steps = steps,
            IsHappyPath = isHappyPath
        };

    private static DefaultDiagramsFetcher.DiagramAsCode[] MakeDiagrams(params (string testId, string plantuml)[] diagrams) =>
        diagrams.Select(d => new DefaultDiagramsFetcher.DiagramAsCode(d.testId, "", d.plantuml)).ToArray();

    private string GenerateReport(Feature[] features, DefaultDiagramsFetcher.DiagramAsCode[]? diagrams = null, string? fileName = null,
        bool groupParameterizedTests = true, int maxParameterColumns = 10, bool titleizeParameterNames = true)
    {
        diagrams ??= features.SelectMany(f => f.Scenarios).Select(s => new DefaultDiagramsFetcher.DiagramAsCode(s.Id, "", "")).ToArray();
        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName ?? $"ParamGroup_{Guid.NewGuid():N}.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs,
            groupParameterizedTests: groupParameterizedTests,
            maxParameterColumns: maxParameterColumns,
            titleizeParameterNames: titleizeParameterNames);
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

        // Titleized by default: x → X, y → Y
        Assert.Contains(">X</th>", content);
        Assert.Contains(">Y</th>", content);
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
    public void Single_test_case_does_not_show_identical_diagrams_badge()
    {
        var puml = "@startuml\nA -> B: hello\n@enduml";
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" })
        };
        var diagrams = MakeDiagrams(("s1", puml));
        var content = GenerateReport(MakeFeature(scenarios), diagrams);

        Assert.DoesNotContain("class=\"param-diagram-identical-badge\"", content);
        Assert.DoesNotContain("All diagrams identical across test cases", content);
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
        // Titleized by default
        Assert.Contains(">X</th>", content);
        Assert.Contains(">Y</th>", content);
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

    // ── Outline removal ──

    [Fact]
    public void Search_match_style_does_not_use_outline()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" }),
            MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // The row-search-match style should use box-shadow, not outline
        Assert.DoesNotContain("row-search-match { outline:", content);
        Assert.Contains("row-search-match { box-shadow:", content);
    }

    // ── Parameter name titleization ──

    [Fact]
    public void Parameter_names_are_titleized_by_default()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Calc(region: UK, amount: 100)", outlineId: "Calc",
                exampleValues: new() { ["region"] = "UK", ["amount"] = "100" }),
            MakeScenario("s2", "Calc(region: US, amount: 200)", outlineId: "Calc",
                exampleValues: new() { ["region"] = "US", ["amount"] = "200" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains(">Region</th>", content);
        Assert.Contains(">Amount</th>", content);
        Assert.DoesNotContain(">region</th>", content);
        Assert.DoesNotContain(">amount</th>", content);
    }

    [Fact]
    public void Parameter_names_titleization_handles_camelCase()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(firstName: Jo)", outlineId: "Test",
                exampleValues: new() { ["firstName"] = "Jo" }),
            MakeScenario("s2", "Test(firstName: Al)", outlineId: "Test",
                exampleValues: new() { ["firstName"] = "Al" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains(">First Name</th>", content);
    }

    [Fact]
    public void Parameter_names_titleization_can_be_disabled()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Calc(region: UK)", outlineId: "Calc",
                exampleValues: new() { ["region"] = "UK" }),
            MakeScenario("s2", "Calc(region: US)", outlineId: "Calc",
                exampleValues: new() { ["region"] = "US" })
        };
        var content = GenerateReport(MakeFeature(scenarios), titleizeParameterNames: false);

        Assert.Contains(">region</th>", content);
        Assert.DoesNotContain(">Region</th>", content);
    }

    // ── Single-instance parameterized scenarios use table format ──

    [Fact]
    public void Single_scenario_with_parameters_renders_as_parameterized_group()
    {
        var scenario = MakeScenario("s1", "Process(region: UK, amount: 100)",
            outlineId: "Process",
            exampleValues: new() { ["region"] = "UK", ["amount"] = "100" });
        var content = GenerateReport(MakeFeature(scenario));

        Assert.Contains("scenario-parameterized", content);
        Assert.Contains("param-test-table", content);
    }

    [Fact]
    public void Single_scenario_with_parsed_parameters_renders_as_parameterized_group()
    {
        var scenario = MakeScenario("s1", "Calculate(x: 10, y: 20)");
        var content = GenerateReport(MakeFeature(scenario));

        Assert.Contains("scenario-parameterized", content);
        Assert.Contains("param-test-table", content);
    }

    [Fact]
    public void Single_scenario_with_parameters_shows_titleized_column_headers()
    {
        var scenario = MakeScenario("s1", "Process(region: UK, amount: 100)",
            outlineId: "Process",
            exampleValues: new() { ["region"] = "UK", ["amount"] = "100" });
        var content = GenerateReport(MakeFeature(scenario));

        Assert.Contains(">Region</th>", content);
        Assert.Contains(">Amount</th>", content);
    }

    [Fact]
    public void Single_scenario_without_parameters_renders_normally()
    {
        var scenario = MakeScenario("s1", "Create order");
        var content = GenerateReport(MakeFeature(scenario));

        // Should be a regular scenario, not a parameterized group
        Assert.DoesNotContain("<details class=\"scenario scenario-parameterized\"", content);
        Assert.DoesNotContain("<table class=\"param-test-table\"", content);
    }

    [Fact]
    public void Single_scenario_with_bracket_parameters_renders_as_parameterized_group()
    {
        var scenario = MakeScenario("s1", "Scenario name [count: 5]");
        var content = GenerateReport(MakeFeature(scenario));

        Assert.Contains("scenario-parameterized", content);
        Assert.Contains("param-test-table", content);
    }

    [Fact]
    public void Parameterized_group_with_happy_path_scenarios_gets_happy_path_class()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Reset(version: V1)", outlineId: "Reset",
                exampleValues: new() { ["version"] = "V1" }, isHappyPath: true),
            MakeScenario("s2", "Reset(version: V2)", outlineId: "Reset",
                exampleValues: new() { ["version"] = "V2" }, isHappyPath: true)
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("scenario-parameterized happy-path", content);
        Assert.Contains(">Happy Path</span>", content);
    }

    [Fact]
    public void Parameterized_group_without_happy_path_scenarios_does_not_get_happy_path_class()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Reset(version: V1)", outlineId: "Reset",
                exampleValues: new() { ["version"] = "V1" }),
            MakeScenario("s2", "Reset(version: V2)", outlineId: "Reset",
                exampleValues: new() { ["version"] = "V2" })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.DoesNotContain("scenario-parameterized happy-path", content);
        Assert.DoesNotContain(">Happy Path</span>", content);
    }

    [Fact]
    public void Parameterized_group_with_mixed_happy_path_gets_happy_path_class()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Reset(version: V1)", outlineId: "Reset",
                exampleValues: new() { ["version"] = "V1" }, isHappyPath: true),
            MakeScenario("s2", "Reset(version: V2)", outlineId: "Reset",
                exampleValues: new() { ["version"] = "V2" }, isHappyPath: false)
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("scenario-parameterized happy-path", content);
    }

    // ── R2: FlattenedObject rendering ──

    private record MerchantRequest(string Region, int Amount, string Currency);

    [Fact]
    public void R2_flattened_object_renders_property_columns()
    {
        var obj1 = new MerchantRequest("UK", 100, "GBP");
        var obj2 = new MerchantRequest("US", 200, "USD");
        var scenarios = new[]
        {
            MakeScenario("s1", "Process(request)", outlineId: "Process",
                exampleValues: new() { ["request"] = obj1.ToString()! },
                exampleRawValues: new() { ["request"] = obj1 }),
            MakeScenario("s2", "Process(request)", outlineId: "Process",
                exampleValues: new() { ["request"] = obj2.ToString()! },
                exampleRawValues: new() { ["request"] = obj2 })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // Should show flattened property columns
        Assert.Contains("Input Parameters", content);
        Assert.Contains(">Region</th>", content);
        Assert.Contains(">Amount</th>", content);
        Assert.Contains(">Currency</th>", content);
        // Should show flattened values
        Assert.Contains(">UK</td>", content);
        Assert.Contains(">100</td>", content);
        Assert.Contains(">GBP</td>", content);
    }

    // ── R3: SubTable cell rendering ──

    private record SmallAddress(string Street, string City, string PostCode);

    [Fact]
    public void R3_small_complex_object_renders_subtable_in_cell()
    {
        var addr = new SmallAddress("1 Main St", "London", "SW1A 1AA");
        var scenarios = new[]
        {
            MakeScenario("s1", "Send(amount: 100, address: ...)", outlineId: "Send",
                exampleValues: new() { ["amount"] = "100", ["address"] = addr.ToString()! },
                exampleRawValues: new() { ["amount"] = (object)100, ["address"] = addr }),
            MakeScenario("s2", "Send(amount: 200, address: ...)", outlineId: "Send",
                exampleValues: new() { ["amount"] = "200", ["address"] = new SmallAddress("2 Oak Ave", "Paris", "75001").ToString()! },
                exampleRawValues: new() { ["amount"] = (object)200, ["address"] = new SmallAddress("2 Oak Ave", "Paris", "75001") })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("cell-subtable", content);
        Assert.Contains(">Street</th>", content);
        Assert.Contains(">City</th>", content);
        Assert.Contains(">PostCode</th>", content);
        Assert.Contains("1 Main St", content);
    }

    // ── R4: ExpandableComplex cell rendering ──

    private record ComplexBatch(string Currency, string Priority, List<string> Payments);

    [Fact]
    public void R4_deeply_complex_object_renders_expandable_details()
    {
        var batch = new ComplexBatch("GBP", "Normal", ["P001", "P002"]);
        var scenarios = new[]
        {
            MakeScenario("s1", "Process(id: batch-001, batch: ...)", outlineId: "Process",
                exampleValues: new() { ["id"] = "batch-001", ["batch"] = batch.ToString()! },
                exampleRawValues: new() { ["id"] = (object)"batch-001", ["batch"] = batch }),
            MakeScenario("s2", "Process(id: batch-002, batch: ...)", outlineId: "Process",
                exampleValues: new() { ["id"] = "batch-002", ["batch"] = batch.ToString()! },
                exampleRawValues: new() { ["id"] = (object)"batch-002", ["batch"] = batch })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("param-expand", content);
        Assert.Contains("expand-body", content);
        Assert.Contains("prop-key", content);
        Assert.Contains("prop-val", content);
        // The scalar column "id" should still be plain text
        Assert.Contains(">batch-001</td>", content);
    }

    // ── R4: CSS and JS present ──

    [Fact]
    public void Report_contains_param_expand_css()
    {
        var scenarios = new[] { MakeScenario("s1", "Test") };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("details.param-expand", content);
        Assert.Contains(".expand-body", content);
        Assert.Contains(".prop-key", content);
        Assert.Contains(".prop-val", content);
    }

    [Fact]
    public void Report_contains_cell_subtable_css()
    {
        var scenarios = new[] { MakeScenario("s1", "Test") };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains(".cell-subtable", content);
    }

    [Fact]
    public void Report_contains_param_expand_js()
    {
        var scenarios = new[] { MakeScenario("s1", "Test") };
        var content = GenerateReport(MakeFeature(scenarios));

        Assert.Contains("param-expand", content);
        Assert.Contains("cell-subtable", content);
        Assert.Contains("stopPropagation", content);
    }

    // ── Nested object renders expandable ──

    private record NestedPayment(string Name, SmallAddress Address);

    [Fact]
    public void Nested_complex_object_renders_as_R4_expandable()
    {
        var nested = new NestedPayment("Acme", new SmallAddress("1 Main St", "London", "SW1A 1AA"));
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(target: ...)", outlineId: "Test",
                exampleValues: new() { ["target"] = nested.ToString()! },
                exampleRawValues: new() { ["target"] = nested }),
            MakeScenario("s2", "Test(target: ...)", outlineId: "Test",
                exampleValues: new() { ["target"] = nested.ToString()! },
                exampleRawValues: new() { ["target"] = nested })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // Nested object should use R4 expandable, not R3 sub-table
        Assert.Contains("param-expand", content);
        Assert.Contains("NestedPayment", content);
    }

    // ── R2 not applied when single param is already scalar ──

    [Fact]
    public void Single_scalar_param_uses_ScalarColumns_not_FlattenedObject()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Test(value: 42)", outlineId: "Test",
                exampleValues: new() { ["value"] = "42" },
                exampleRawValues: new() { ["value"] = (object)42 }),
            MakeScenario("s2", "Test(value: 99)", outlineId: "Test",
                exampleValues: new() { ["value"] = "99" },
                exampleRawValues: new() { ["value"] = (object)99 })
        };
        var content = GenerateReport(MakeFeature(scenarios));

        // Should render as normal scalar column, not flattened
        Assert.Contains("Input Parameters", content);
        Assert.Contains(">Value</th>", content);
        Assert.Contains(">42</td>", content);
    }

    [Fact]
    public void Steps_in_detail_panel_use_details_summary_pattern()
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
        // Should use <details> with <summary>, not a plain <div>
        Assert.Contains("<details class=\"scenario-steps\" open>", content);
        Assert.Contains("<summary class=\"h4\">Steps</summary>", content);
        Assert.DoesNotContain("<div class=\"scenario-steps\">", content);
    }
}
