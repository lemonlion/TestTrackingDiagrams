using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for the copy scenario name feature.
/// A small copy button next to each scenario title for pasting into bug reports.
/// </summary>
public class CopyScenarioNameReportTests
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
    public void Report_scenario_contains_copy_button()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CopyScenarioBtn.html");
        Assert.Contains("copy-scenario-name", content);
    }

    [Fact]
    public void Report_contains_copy_javascript()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CopyScenarioJs.html");
        Assert.Contains("copy_scenario_name", content);
    }

    [Fact]
    public void Report_copy_button_uses_clipboard_api()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CopyScenarioClipboard.html");
        Assert.Contains("navigator.clipboard", content);
    }

    [Fact]
    public void Report_copy_button_has_title_attribute()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CopyScenarioTitle.html");
        Assert.Contains("title=\"Copy scenario name\"", content);
    }

    [Fact]
    public void Report_copy_button_does_not_trigger_scenario_toggle()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CopyScenarioNoToggle.html");
        Assert.Contains("stopPropagation", content);
    }
}
