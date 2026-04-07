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
        Assert.Contains("step-param-table", content);
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
        Assert.DoesNotContain("<div class=\"step-param-table\">", content);
        Assert.DoesNotContain("<div class=\"step-param-tree\">", content);
    }
}
