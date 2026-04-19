using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class SearchDataAttributeTests
{
    private static string GenerateReport(Feature[] features)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SearchDataAttr.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    private static string DecompressBase64(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    private static string[] ExtractDataSearchValues(string html)
    {
        return Regex.Matches(html, @"data-search-z=""([^""]*)""\s")
            .Select(m => DecompressBase64(m.Groups[1].Value))
            .ToArray();
    }

    [Fact]
    public void Data_search_includes_step_text()
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
                        Id = "s1", DisplayName = "Create order", Result = ExecutionResult.Passed,
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "a valid order request with zuplotzky widget", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "I submit the order", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "Then", Text = "the order is confirmed", Status = ExecutionResult.Passed }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        // The data-search attribute for scenario s1 should contain step text
        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("create order"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("zuplotzky widget", scenarioSearch);
        Assert.Contains("submit the order", scenarioSearch);
        Assert.Contains("order is confirmed", scenarioSearch);
    }

    [Fact]
    public void Data_search_includes_substep_text()
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
                        Id = "s1", DisplayName = "Nested steps", Result = ExecutionResult.Passed,
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "a parent step", Status = ExecutionResult.Passed,
                                SubSteps =
                                [
                                    new ScenarioStep { Keyword = "And", Text = "a deeply nested frobnicator step", Status = ExecutionResult.Passed }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("nested steps"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("deeply nested frobnicator", scenarioSearch);
    }

    [Fact]
    public void Data_search_for_outline_includes_step_text()
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
                        Id = "s1", DisplayName = "Withdraw $200", Result = ExecutionResult.Passed,
                        OutlineId = "withdraw-cash",
                        ExampleValues = new Dictionary<string, string> { ["Amount"] = "$200" },
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the xylophonic account has funds", Status = ExecutionResult.Passed }
                        ]
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Withdraw $500", Result = ExecutionResult.Passed,
                        OutlineId = "withdraw-cash",
                        ExampleValues = new Dictionary<string, string> { ["Amount"] = "$500" },
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the xylophonic account has funds", Status = ExecutionResult.Passed }
                        ]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var outlineSearch = searchValues.FirstOrDefault(v => v.Contains("withdraw"));
        Assert.NotNull(outlineSearch);
        Assert.Contains("xylophonic account", outlineSearch);
    }
}
