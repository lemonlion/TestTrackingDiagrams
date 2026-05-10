using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ParameterRenderingReportTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "ParamRender.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_renders_inline_parameter()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "user named <name>",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "name",
                                        Kind = StepParameterKind.Inline,
                                        InlineValue = new InlineParameterValue("Alice", null, VerificationStatus.NotApplicable)
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("step-param-inline", content);
        Assert.Contains("Alice", content);
    }

    [Fact]
    public void Report_renders_inline_parameter_with_expectation()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "result is <expected>",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "expected",
                                        Kind = StepParameterKind.Inline,
                                        InlineValue = new InlineParameterValue("42", "42", VerificationStatus.Success)
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("param-success", content);
    }

    [Fact]
    public void Report_renders_tabular_parameter()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "users",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "users",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Name", false), new TabularColumn("Age", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Alice", null, VerificationStatus.NotApplicable),
                                                 new TabularCell("30", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.DoesNotContain("<div class=\"step-param-combined-table\"", content);
        Assert.Contains("step-param-table", content);
        Assert.Contains("Alice", content);
        Assert.Contains("Age", content);
        // All rows are Matching → no row indicator column
        Assert.DoesNotContain("<td>=</td>", content);
    }

    [Fact]
    public void Report_renders_tree_parameter()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "result matches",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "result",
                                        Kind = StepParameterKind.Tree,
                                        TreeValue = new TreeParameterValue(
                                            new TreeNode("$", "root", "obj", null, VerificationStatus.NotApplicable,
                                                [new TreeNode("$.name", "name", "Alice", "Alice", VerificationStatus.Success, null)]))
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("step-param-tree", content);
        Assert.Contains("Alice", content);
    }

    [Fact]
    public void Report_does_not_render_parameters_when_none()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "something" }]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.DoesNotContain("<span class=\"step-param-inline", content);
        Assert.DoesNotContain("<div class=\"step-param-combined-table\">", content);
        Assert.DoesNotContain("<div class=\"step-param-tree\">", content);
    }

    [Fact]
    public void Tabular_parameters_strip_param_suffix_from_step_text()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "invalid label requests with invalid fields [invalidFields: \"<$invalidFields>\"]",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "invalidFields",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Field", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Name", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("invalid label requests with invalid fields", content);
        Assert.DoesNotContain("[invalidFields:", content);
        Assert.DoesNotContain("&lt;$invalidFields&gt;", content);
    }

    [Fact]
    public void Tabular_parameters_from_multiple_steps_are_combined_after_steps()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "requests with data [inputs: \"<$inputs>\"]",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "inputs",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Field", false), new TabularColumn("Value", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Name", null, VerificationStatus.NotApplicable),
                                                 new TabularCell("Alice", null, VerificationStatus.NotApplicable)]),
                                             new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Age", null, VerificationStatus.NotApplicable),
                                                 new TabularCell("30", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            },
                            new ScenarioStep
                            {
                                Keyword = "When", Text = "the requests are sent"
                            },
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "the responses should match [expectedOutputs: \"<$expectedOutputs>\"]",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "expectedOutputs",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Status", false), new TabularColumn("Message", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("200", "200", VerificationStatus.Success),
                                                 new TabularCell("OK", "OK", VerificationStatus.Success)]),
                                             new TabularRow(TableRowType.Matching,
                                                [new TabularCell("400", "400", VerificationStatus.Success),
                                                 new TabularCell("Bad", "Bad", VerificationStatus.Success)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);

        // The combined table should appear after the steps, not inline with each step
        Assert.Contains("step-param-combined-table", content);

        // Input columns should appear
        Assert.Contains("Field", content);
        Assert.Contains("Value", content);

        // Separator column
        Assert.Contains("<th class=\"combined-separator\">=</th>", content);

        // Output columns should appear
        Assert.Contains("Status", content);
        Assert.Contains("Message", content);

        // Data from both tables
        Assert.Contains("Alice", content);
        Assert.Contains("OK", content);

        // Should not have individual step-param-table divs for the tabular params
        Assert.DoesNotContain("<div class=\"step-param-table\">", content);

        // Combined table should be inside the scenario-steps details, not after it
        var stepsIdx = content.IndexOf("class=\"scenario-steps\"");
        var combinedIdx = content.IndexOf("<div class=\"step-param-combined-table\">");
        Assert.True(combinedIdx > stepsIdx,
            "Combined table div should appear after scenario-steps opening");
        // Find the next </details> after the combined table — it should be the scenario-steps closer
        var closingAfterCombined = content.IndexOf("</details>", combinedIdx);
        // The scenario-steps details should close right after the combined table
        // Verify no other <details> opens between the combined table and that </details>
        var intervening = content.Substring(combinedIdx, closingAfterCombined - combinedIdx);
        Assert.DoesNotContain("<details", intervening);
    }

    [Fact]
    public void Combined_table_cells_have_data_param_attributes()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "requests",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "inputs",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Field", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Name", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            },
                            new ScenarioStep { Keyword = "When", Text = "sent" },
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "should match",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "expectedOutputs",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Status", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("200", "200", VerificationStatus.Success)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);

        // Combined table cells should have data-param attributes for JS highlighting
        Assert.Contains("data-param=\"inputs\"", content);
        Assert.Contains("data-param=\"expectedOutputs\"", content);
    }

    [Fact]
    public void Combined_table_preserves_verification_status_css_classes()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "input data",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "data",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Name", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Alice", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            },
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "result matches",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "result",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Status", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Fail", "Pass", VerificationStatus.Failure)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("param-failure", content);
        Assert.Contains("Fail/Pass", content);
        // All rows are Matching → no row indicator column in combined table
        Assert.DoesNotContain("<td>=</td>", content);
    }

    [Fact]
    public void Single_tabular_parameter_renders_as_combined_table_without_separator()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "users",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "users",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Name", false), new TabularColumn("Age", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Alice", null, VerificationStatus.NotApplicable),
                                                 new TabularCell("30", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            },
                            new ScenarioStep
                            {
                                Keyword = "When", Text = "they are saved"
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.DoesNotContain("<div class=\"step-param-combined-table\"", content);
        Assert.Contains("step-param-table", content);
        Assert.Contains("Alice", content);
    }

    [Fact]
    public void Combined_table_handles_row_type_indicators_from_output_table()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "input data",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "data",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Name", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Alice", null, VerificationStatus.NotApplicable)]),
                                             new TabularRow(TableRowType.Matching,
                                                [new TabularCell("Bob", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            },
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "results match",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "result",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Status", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("OK", "OK", VerificationStatus.Success)]),
                                             new TabularRow(TableRowType.Surplus,
                                                [new TabularCell("Extra", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("row-surplus", content);
        Assert.Contains("+", content);
        // Mixed row types → indicator column IS present
        Assert.Contains("<td>=</td>", content);
        Assert.Contains("<td>+</td>", content);
    }

    [Fact]
    public void Step_param_table_left_margin_aligns_with_step_text()
    {
        // The step-param-table should be indented to align with the start of the step text,
        // not with the tick/status indicator. The text starts after: ::before (0.3em) +
        // status margin-left (0.5em) + status width (1.2em) + status margin-right (0.3em) = ~2.3em
        Assert.Contains(".step-param-table", Stylesheets.HtmlReportStyleSheet);
        Assert.Contains("margin: 4px 0 4px 2.3em", Stylesheets.HtmlReportStyleSheet);
    }

    [Fact]
    public void Report_renders_null_tabular_cell_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "data",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "data",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Value", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("null", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<pre>null</pre>", content);
    }

    [Fact]
    public void Report_renders_empty_string_tabular_cell_without_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "data",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "data",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Value", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.DoesNotContain("<pre>null</pre>", content);
    }

    [Fact]
    public void Report_renders_null_inline_parameter_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "value is <v>",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "v",
                                        Kind = StepParameterKind.Inline,
                                        InlineValue = new InlineParameterValue("null", null, VerificationStatus.NotApplicable)
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<pre>null</pre>", content);
    }

    [Fact]
    public void Report_renders_null_tree_node_value_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "result matches",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "result",
                                        Kind = StepParameterKind.Tree,
                                        TreeValue = new TreeParameterValue(
                                            new TreeNode("$", "root", "null", null, VerificationStatus.NotApplicable, null))
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<pre>null</pre>", content);
    }

    [Fact]
    public void Report_renders_null_combined_table_cell_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "input data",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "data",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Name", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("null", null, VerificationStatus.NotApplicable)])])
                                    }
                                ]
                            },
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "results match",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "result",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Status", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("OK", "OK", VerificationStatus.Success)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("step-param-combined-table", content);
        Assert.Contains("<pre>null</pre>", content);
    }

    [Fact]
    public void Report_renders_null_step_text_segment_parameter_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "value is <v>",
                                TextSegments =
                                [
                                    StepTextSegment.Literal("value is "),
                                    StepTextSegment.Param("v", new InlineParameterValue("null", null, VerificationStatus.NotApplicable))
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<pre>null</pre>", content);
    }

    [Fact]
    public void Report_renders_null_expectation_in_failure_cell_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Then", Text = "results match",
                                Parameters =
                                [
                                    new StepParameter
                                    {
                                        Name = "result",
                                        Kind = StepParameterKind.Tabular,
                                        TabularValue = new TabularParameterValue(
                                            [new TabularColumn("Value", false)],
                                            [new TabularRow(TableRowType.Matching,
                                                [new TabularCell("hello", "null", VerificationStatus.Failure)])])
                                    }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("hello/<pre>null</pre>", content);
    }

    [Fact]
    public void Input_parameter_table_renders_null_as_pre_null()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test(null)",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["value"] = "null" }
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Test(hello)",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["value"] = "hello" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<pre>null</pre>", content);
    }

    [Fact]
    public void Input_parameter_table_renders_whitespace_in_pre_element()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test( )",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["value"] = " " }
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Test(hello)",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["value"] = "hello" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<pre> </pre>", content);
    }

    [Fact]
    public void Input_parameter_table_renders_empty_string_without_pre()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test()",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["value"] = "" }
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Test(hello)",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["value"] = "hello" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.DoesNotContain("<pre>null</pre>", content);
        Assert.DoesNotContain("<pre></pre>", content);
        Assert.DoesNotContain("<pre> </pre>", content);
    }
}
