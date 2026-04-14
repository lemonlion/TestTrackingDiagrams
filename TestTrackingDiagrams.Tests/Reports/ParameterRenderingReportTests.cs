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
        Assert.Contains("step-param-combined-table", content);
        Assert.Contains("Alice", content);
        Assert.Contains("Age", content);
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
        Assert.Contains("step-param-combined-table", content);
        Assert.DoesNotContain("<th class=\"combined-separator\">=</th>", content);
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
    }
}
