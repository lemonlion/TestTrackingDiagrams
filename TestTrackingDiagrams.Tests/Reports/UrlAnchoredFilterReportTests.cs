using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for the URL-anchored filters feature.
/// Filter state should be encoded in the URL hash so filtered views can be shared via link.
/// </summary>
public class UrlAnchoredFilterReportTests
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
    public void Report_contains_update_url_hash_function()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "UrlHashUpdate.html");
        Assert.Contains("update_url_hash", content);
    }

    [Fact]
    public void Report_contains_parse_url_hash_function()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "UrlHashParse.html");
        Assert.Contains("parse_url_hash", content);
    }

    [Fact]
    public void Report_updates_hash_on_filter_change()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "UrlHashOnFilter.html");
        Assert.Contains("history.replaceState", content);
    }

    [Fact]
    public void Report_restores_filters_from_hash_on_load()
    {
        var features = MakeFeatures(("t1", "Create order", ScenarioResult.Passed));
        var content = GenerateReport(features, "UrlHashRestore.html");
        // On load, should parse hash and apply filters
        Assert.Contains("parse_url_hash", content);
    }
}
