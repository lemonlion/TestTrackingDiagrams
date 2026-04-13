using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests that localStorage persistence has been removed from the report.
/// Filters are only restored via URL hash, not localStorage.
/// </summary>
public class PersistentFilterStateReportTests
{
    private static Feature[] MakeFeatures(params (string id, string name, ExecutionResult result)[] scenarios) =>
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
    public void Report_does_not_use_localstorage()
    {
        var features = MakeFeatures(("t1", "Create order", ExecutionResult.Passed));
        var content = GenerateReport(features, "NoLocalStorage.html");
        Assert.DoesNotContain("localStorage", content);
        Assert.DoesNotContain("ttd-filter-state", content);
    }

    [Fact]
    public void Report_does_not_call_restore_filter_state_on_load()
    {
        var features = MakeFeatures(("t1", "Create order", ExecutionResult.Passed));
        var content = GenerateReport(features, "NoRestoreOnLoad.html");
        // restore_filter_state exists as no-op stub but is never called in DOMContentLoaded
        Assert.DoesNotContain("restore_filter_state();", content);
    }

    [Fact]
    public void Report_still_parses_url_hash_on_load()
    {
        var features = MakeFeatures(("t1", "Create order", ExecutionResult.Passed));
        var content = GenerateReport(features, "UrlHashOnLoad.html");
        Assert.Contains("parse_url_hash", content);
    }
}
