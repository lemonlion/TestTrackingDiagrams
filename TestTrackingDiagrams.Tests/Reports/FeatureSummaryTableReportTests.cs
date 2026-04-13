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
                    new Scenario { Id = "s1", DisplayName = "Place order", Result = ExecutionResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "Cancel order", Result = ExecutionResult.Failed, ErrorMessage = "err" }
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
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Failed, ErrorMessage = "err" },
                    new Scenario { Id = "s3", DisplayName = "S3", Result = ExecutionResult.Skipped }
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
                            new ScenarioStep { Keyword = "Given", Text = "x", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "y", Status = ExecutionResult.Failed }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("Steps", content);
    }

    [Fact]
    public void Summary_table_shows_duration_columns()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(2) }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains(">Duration<", content);
        Assert.Contains(">Avg<", content);
        Assert.Contains(">Longest<", content);
    }

    [Fact]
    public void Summary_table_duration_shows_sum_of_scenario_durations()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1) },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(2) },
                    new Scenario { Id = "s3", DisplayName = "S3", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(3) }
                ]
            }
        };

        var content = GenerateReport(features);
        // Sum is 6s
        Assert.Contains("6s", content);
    }

    [Fact]
    public void Summary_table_avg_shows_mean_scenario_duration()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1) },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(2) },
                    new Scenario { Id = "s3", DisplayName = "S3", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(3) }
                ]
            }
        };

        var content = GenerateReport(features);
        // Avg is 2s
        Assert.Contains("2s", content);
    }

    [Fact]
    public void Summary_table_longest_shows_max_scenario_duration()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1) },
                    new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(5) },
                    new Scenario { Id = "s3", DisplayName = "S3", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(3) }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("5s", content);
    }

    [Fact]
    public void Summary_table_omits_duration_columns_when_no_durations()
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
        Assert.DoesNotContain(">Duration<", content);
        Assert.DoesNotContain(">Avg<", content);
        Assert.DoesNotContain(">Longest<", content);
    }
}
