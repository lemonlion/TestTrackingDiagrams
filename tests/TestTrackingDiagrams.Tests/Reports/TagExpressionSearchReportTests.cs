using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class TagExpressionSearchReportTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "TagExprSearch.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Search_bar_placeholder_mentions_tags()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("@tag", content);
    }

    [Fact]
    public void Search_includes_tag_expression_parser_js()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("evaluateTagExpression", content);
    }

    [Fact]
    public void Scenarios_have_data_labels_attribute()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Labels = ["important", "regression"] }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("data-labels=\"important,regression\"", content);
    }

    [Fact]
    public void Tag_expression_supports_and_or_not()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
            }
        };

        var content = GenerateReport(features);
        // Check the parser handles the three operators in parsing functions
        Assert.Contains("parseAnd", content);
        Assert.Contains("parseOr", content);
        Assert.Contains("parseNot", content);
    }

    [Fact]
    public void Tag_expression_supports_parentheses()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("lparen", content);
        Assert.Contains("rparen", content);
    }
}
