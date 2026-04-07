using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for the persistent filter state feature.
/// Filter selections should be saved to localStorage and restored on page load.
/// </summary>
public class PersistentFilterStateReportTests
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
    public void Report_contains_save_filter_state_function()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "PersistFilterSave.html");
        Assert.Contains("save_filter_state", content);
    }

    [Fact]
    public void Report_contains_restore_filter_state_function()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "PersistFilterRestore.html");
        Assert.Contains("restore_filter_state", content);
    }

    [Fact]
    public void Report_filter_state_uses_localstorage()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "PersistFilterStorage.html");
        Assert.Contains("localStorage", content);
        Assert.Contains("ttd-filter-state", content);
    }

    [Fact]
    public void Report_restores_filter_state_on_domcontentloaded()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "PersistFilterOnLoad.html");
        Assert.Contains("DOMContentLoaded", content);
        Assert.Contains("restore_filter_state", content);
    }
}
