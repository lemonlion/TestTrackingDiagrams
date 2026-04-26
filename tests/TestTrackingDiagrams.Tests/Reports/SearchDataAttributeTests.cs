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

    private static string[] ExtractDataSearchValues(string html)
    {
        return Regex.Matches(html, @"data-search=""([^""]*)""")
            .Select(m => m.Groups[1].Value)
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

    [Fact]
    public void Data_search_includes_feature_display_name()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Pancakes Creation",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Create a basic pancake", Result = ExecutionResult.Passed,
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "some batter", Status = ExecutionResult.Passed }]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("create a basic pancake"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("pancakes creation", scenarioSearch);
    }

    [Fact]
    public void Data_search_includes_feature_description()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Description = "This feature covers zorblatt integration",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test scenario", Result = ExecutionResult.Passed
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("test scenario"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("zorblatt integration", scenarioSearch);
    }

    [Fact]
    public void Data_search_includes_feature_labels()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Labels = ["xylophage", "frobnicate"],
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Test scenario", Result = ExecutionResult.Passed
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("test scenario"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("xylophage", scenarioSearch);
        Assert.Contains("frobnicate", scenarioSearch);
    }

    [Fact]
    public void Data_search_includes_scenario_categories()
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
                        Id = "s1", DisplayName = "Test scenario", Result = ExecutionResult.Passed,
                        Categories = ["zygomorphic", "plumbus"]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("test scenario"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("zygomorphic", scenarioSearch);
        Assert.Contains("plumbus", scenarioSearch);
    }

    [Fact]
    public void Data_search_includes_scenario_labels()
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
                        Id = "s1", DisplayName = "Test scenario", Result = ExecutionResult.Passed,
                        Labels = ["quixotic", "barnacle"]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("test scenario"));
        Assert.NotNull(scenarioSearch);
        Assert.Contains("quixotic", scenarioSearch);
        Assert.Contains("barnacle", scenarioSearch);
    }

    [Fact]
    public void Data_search_for_parameterized_group_includes_feature_name()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Waffles Preparation",
                Description = "Covers all waffle scenarios",
                Labels = ["breakfast"],
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Make waffles with sugar", Result = ExecutionResult.Passed,
                        OutlineId = "make-waffles",
                        ExampleValues = new Dictionary<string, string> { ["Topping"] = "sugar" },
                        Categories = ["sweet"],
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "waffle iron is hot", Status = ExecutionResult.Passed }]
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Make waffles with syrup", Result = ExecutionResult.Passed,
                        OutlineId = "make-waffles",
                        ExampleValues = new Dictionary<string, string> { ["Topping"] = "syrup" },
                        Categories = ["sweet"],
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "waffle iron is hot", Status = ExecutionResult.Passed }]
                    }
                ]
            }
        };

        var content = GenerateReport(features);
        var searchValues = ExtractDataSearchValues(content);

        // The parameterized group data-search should contain the feature name, description, labels, and categories
        var groupSearch = searchValues.FirstOrDefault(v => v.Contains("make waffles"));
        Assert.NotNull(groupSearch);
        Assert.Contains("waffles preparation", groupSearch);
        Assert.Contains("covers all waffle scenarios", groupSearch);
        Assert.Contains("breakfast", groupSearch);
        Assert.Contains("sweet", groupSearch);
    }
}
