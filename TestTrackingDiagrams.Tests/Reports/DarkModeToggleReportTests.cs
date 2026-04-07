using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for dark mode toggle feature.
/// A toggle button switches between light and dark themes,
/// persisting the preference to localStorage.
/// </summary>
public class DarkModeToggleReportTests
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
    public void Report_contains_dark_mode_toggle_button()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "DarkModeBtn.html");
        Assert.Contains("dark-mode-toggle", content);
    }

    [Fact]
    public void Report_contains_dark_mode_css_variables()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "DarkModeCss.html");
        Assert.Contains("body.dark-mode", content);
    }

    [Fact]
    public void Report_contains_dark_mode_javascript()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "DarkModeJs.html");
        Assert.Contains("toggle_dark_mode", content);
    }

    [Fact]
    public void Report_dark_mode_persists_to_localstorage()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "DarkModeStorage.html");
        Assert.Contains("localStorage", content);
        Assert.Contains("dark-mode", content);
    }

    [Fact]
    public void Report_dark_mode_restores_from_localstorage_on_load()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "DarkModeRestore.html");
        Assert.Contains("getItem", content);
    }
}
