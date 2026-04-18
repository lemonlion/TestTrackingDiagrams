using System.Text.RegularExpressions;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class UrlHashFilterTests
{
    private static Feature[] MakeFeatures() =>
    [
        new Feature
        {
            DisplayName = "Test Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "t1",
                    DisplayName = "Test A",
                    IsHappyPath = true,
                    Result = ExecutionResult.Passed,
                    Duration = TimeSpan.FromSeconds(1),
                    Categories = ["Smoke"]
                }
            ]
        }
    ];

    private static string GenerateReport(string fileName)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], MakeFeatures(),
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName, "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Extracts the body of a JS function by matching braces from the function declaration.
    /// </summary>
    private static string ExtractFunctionBody(string content, string functionName)
    {
        var marker = $"function {functionName}(";
        var idx = content.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(idx >= 0, $"Function '{functionName}' not found in content");

        // Find the opening brace
        var braceStart = content.IndexOf('{', idx);
        Assert.True(braceStart >= 0);

        var depth = 0;
        for (var i = braceStart; i < content.Length; i++)
        {
            if (content[i] == '{') depth++;
            else if (content[i] == '}') depth--;
            if (depth == 0) return content[braceStart..(i + 1)];
        }
        throw new Exception($"Unmatched braces in function '{functionName}'");
    }

    // --- Bug 1: All filter functions should call update_url_hash() ---

    [Fact]
    public void Filter_statuses_calls_update_url_hash()
    {
        var content = GenerateReport("UrlHashStatus.html");
        var body = ExtractFunctionBody(content, "filter_statuses");
        Assert.Contains("update_url_hash()", body);
    }

    [Fact]
    public void Filter_happy_paths_calls_update_url_hash()
    {
        var content = GenerateReport("UrlHashHappyPath.html");
        var body = ExtractFunctionBody(content, "filter_happy_paths");
        Assert.Contains("update_url_hash()", body);
    }

    [Fact]
    public void Run_search_scenarios_calls_update_url_hash()
    {
        var content = GenerateReport("UrlHashSearch.html");
        var body = ExtractFunctionBody(content, "run_search_scenarios");
        Assert.Contains("update_url_hash()", body);
    }

    [Fact]
    public void Filter_dependencies_calls_update_url_hash()
    {
        var content = GenerateReport("UrlHashDeps.html");
        var body = ExtractFunctionBody(content, "filter_dependencies");
        Assert.Contains("update_url_hash()", body);
    }

    [Fact]
    public void Filter_categories_calls_update_url_hash()
    {
        var content = GenerateReport("UrlHashCategories.html");
        var body = ExtractFunctionBody(content, "filter_categories");
        Assert.Contains("update_url_hash()", body);
    }

    // --- Bug 1: update_url_hash should save categories ---

    [Fact]
    public void Update_url_hash_saves_categories()
    {
        var content = GenerateReport("UrlHashSavesCats.html");
        var body = ExtractFunctionBody(content, "update_url_hash");
        Assert.Contains("category-toggle.category-active", body);
    }

    // --- Bug 2: parse_url_hash should restore categories ---

    [Fact]
    public void Parse_url_hash_restores_categories()
    {
        var content = GenerateReport("UrlHashRestoresCats.html");
        var body = ExtractFunctionBody(content, "parse_url_hash");
        Assert.Contains("params.cats", body);
    }
}
