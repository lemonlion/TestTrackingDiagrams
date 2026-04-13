using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CategoryMultiSelectReportTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "CategoryMultiSelect.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Category_filter_has_and_or_toggle()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Categories = ["smoke"] }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("cat-mode-toggle", content);
    }

    [Fact]
    public void Category_filter_and_mode_requires_all_categories()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Categories = ["smoke", "api"] }
                ]
            }
        };

        var content = GenerateReport(features);
        // In AND mode, the filter checks all active categories are present
        Assert.Contains("_catMode === 'AND'", content);
        Assert.Contains("allMatch", content);
    }

    [Fact]
    public void Category_filter_or_mode_matches_any_category()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Categories = ["smoke"] }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("var _catMode = 'OR'", content);
    }

    [Fact]
    public void Category_mode_persisted_in_url_hash()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Categories = ["smoke"] }
                ]
            }
        };

        var content = GenerateReport(features);
        Assert.Contains("catmode", content);
    }
}
