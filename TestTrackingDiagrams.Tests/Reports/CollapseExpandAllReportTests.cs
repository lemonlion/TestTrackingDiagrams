using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for the Collapse/Expand All button feature.
/// A single button should collapse or expand all features/scenarios at once.
/// </summary>
public class CollapseExpandAllReportTests
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
    public void Report_contains_collapse_expand_all_button()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CollapseExpandBtn.html");
        Assert.Contains("collapse-expand-all", content);
    }

    [Fact]
    public void Report_contains_collapse_expand_javascript()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CollapseExpandJs.html");
        Assert.Contains("toggle_expand_collapse", content);
    }

    [Fact]
    public void Report_collapse_expand_buttons_have_correct_text()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "CollapseExpandDefault.html");
        Assert.Contains("Expand All", content);
    }
}
