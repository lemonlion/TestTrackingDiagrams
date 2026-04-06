using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class StatusFilterReportGeneratorTests
{
    private static Feature[] MakeFeatures(params (string id, string name, ScenarioResult result)[] scenarios) =>
    [
        new Feature
        {
            DisplayName = "Test Feature",
            Scenarios = scenarios.Select(s => new Scenario
            {
                Id = s.id,
                DisplayName = s.name,
                IsHappyPath = false,
                Result = s.result
            }).ToArray()
        }
    ];

    private static string GenerateReport(Feature[] features, string fileName)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName, "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_contains_status_filter_container()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "StatusFilterContainer.html");
        Assert.Contains("status-filters", content);
    }

    [Fact]
    public void Report_contains_passed_toggle_button()
    {
        var features = MakeFeatures(
            ("t1", "Create order", ScenarioResult.Passed),
            ("t2", "Fail order", ScenarioResult.Failed));
        var content = GenerateReport(features, "StatusFilterPassedBtn.html");
        Assert.Contains("data-status=\"Passed\"", content);
    }

    [Fact]
    public void Report_contains_failed_toggle_button()
    {
        var features = MakeFeatures(
            ("t1", "Create order", ScenarioResult.Passed),
            ("t2", "Fail order", ScenarioResult.Failed));
        var content = GenerateReport(features, "StatusFilterFailedBtn.html");
        Assert.Contains("data-status=\"Failed\"", content);
    }

    [Fact]
    public void Report_only_shows_status_buttons_for_statuses_present()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "StatusFilterOnlyPresent.html");
        Assert.Contains("data-status=\"Passed\"", content);
        // No "Failed" button since no failed scenarios
        Assert.DoesNotContain(">Failed</button>", content);
    }

    [Fact]
    public void Report_scenario_has_data_status_attribute()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "StatusFilterDataAttr.html");
        Assert.Contains("data-status=\"Passed\"", content);
    }

    [Fact]
    public void Report_failed_scenario_has_failed_data_status()
    {
        var features = MakeFeatures(("t1", "Fail order", ScenarioResult.Failed));
        var content = GenerateReport(features, "StatusFilterFailedAttr.html");
        Assert.Matches("class=\"scenario[^\"]*\"[^>]*data-status=\"Failed\"", content);
    }

    [Fact]
    public void Report_contains_status_filter_javascript_function()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "StatusFilterJs.html");
        Assert.Contains("filter_statuses", content);
    }

    [Fact]
    public void Report_contains_status_hidden_css_class()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "StatusFilterCss.html");
        Assert.Contains("status-hidden", content);
    }

    [Fact]
    public void Report_skipped_scenario_has_skipped_data_status()
    {
        var features = MakeFeatures(("t1", "Skip order", ScenarioResult.Skipped));
        var content = GenerateReport(features, "StatusFilterSkippedAttr.html");
        Assert.Matches("data-status=\"Skipped\"", content);
    }

    [Fact]
    public void Report_shows_skipped_toggle_only_when_skipped_scenarios_exist()
    {
        var features = MakeFeatures(
            ("t1", "Create order", ScenarioResult.Passed),
            ("t2", "Fail order", ScenarioResult.Failed));
        var content = GenerateReport(features, "StatusFilterNoSkipped.html");
        Assert.DoesNotContain(">Skipped</button>", content);
    }
}
