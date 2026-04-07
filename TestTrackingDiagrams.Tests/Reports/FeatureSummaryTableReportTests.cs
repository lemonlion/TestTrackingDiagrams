using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class FeatureSummaryTableReportTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SummaryTable.html", "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_contains_feature_summary_table()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "OrderService",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Place order", Result = ScenarioResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "Cancel order", Result = ScenarioResult.Failed, ErrorMessage = "err" }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("feature-summary-table", content);
        Assert.Contains("OrderService", content);
    }

    [Fact]
    public void Summary_table_shows_scenario_counts_by_status()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ScenarioResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ScenarioResult.Failed, ErrorMessage = "err" },
                    new Scenario { Id = "s3", DisplayName = "S3", Result = ScenarioResult.Skipped }
                ]
            }
        };

        var content = GenerateReport(features);
        // Table should contain columns for Passed, Failed, Skipped
        Assert.Contains("Passed", content);
        Assert.Contains("Failed", content);
    }

    [Fact]
    public void Summary_table_is_sortable()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1" }]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("sort_table", content);
    }

    [Fact]
    public void Summary_table_shows_step_counts_when_steps_present()
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
                        Id = "s1", DisplayName = "S1",
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "x", Status = ScenarioResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "y", Status = ScenarioResult.Failed }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("Steps", content);
    }
}
