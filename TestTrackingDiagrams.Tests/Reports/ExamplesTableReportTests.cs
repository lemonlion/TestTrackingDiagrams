using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ExamplesTableReportTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "ExamplesTable.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Scenarios_with_same_outline_id_render_as_examples_table()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Withdraw $200 from $1000", Result = ExecutionResult.Passed,
                        OutlineId = "withdraw-cash",
                        ExampleValues = new Dictionary<string, string> { ["Balance"] = "$1000", ["Withdrawal"] = "$200", ["Result"] = "$800" }
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Withdraw $500 from $1000", Result = ExecutionResult.Passed,
                        OutlineId = "withdraw-cash",
                        ExampleValues = new Dictionary<string, string> { ["Balance"] = "$1000", ["Withdrawal"] = "$500", ["Result"] = "$500" }
                    },
                    new Scenario
                    {
                        Id = "s3", DisplayName = "Withdraw $1500 from $1000", Result = ExecutionResult.Failed, ErrorMessage = "Insufficient",
                        OutlineId = "withdraw-cash",
                        ExampleValues = new Dictionary<string, string> { ["Balance"] = "$1000", ["Withdrawal"] = "$1500", ["Result"] = "Error" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("examples-table", content);
    }

    [Fact]
    public void Examples_table_shows_status_per_row()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["A"] = "1" }
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "S2", Result = ExecutionResult.Failed, ErrorMessage = "err",
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["A"] = "2" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("&#10003;", content); // checkmark for passed
        Assert.Contains("&#10005;", content); // cross for failed
    }

    [Fact]
    public void Scenarios_without_outline_id_render_normally()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.DoesNotContain("<table class=\"examples-table\">", content);
    }

    [Fact]
    public void Examples_table_parameter_names_are_column_headers()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["Balance"] = "$1000", ["Withdrawal"] = "$200" }
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "S2", Result = ExecutionResult.Passed,
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["Balance"] = "$500", ["Withdrawal"] = "$100" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("<th>Balance</th>", content);
        Assert.Contains("<th>Withdrawal</th>", content);
    }

    [Fact]
    public void Mixed_outline_and_regular_scenarios_render_correctly()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Regular", Result = ExecutionResult.Passed },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Outline1", Result = ExecutionResult.Passed,
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["X"] = "1" }
                    },
                    new Scenario
                    {
                        Id = "s3", DisplayName = "Outline2", Result = ExecutionResult.Passed,
                        OutlineId = "outline1",
                        ExampleValues = new Dictionary<string, string> { ["X"] = "2" }
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("examples-table", content);
        Assert.Contains("Regular", content);
    }
}
