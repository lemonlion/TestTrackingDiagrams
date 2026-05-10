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

    [Fact]
    public void Data_search_does_not_contain_plantuml_source()
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
                            new ScenarioStep { Keyword = "Given", Text = "a valid request", Status = ExecutionResult.Passed }
                        ]
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nOrderService -> PaymentGateway : POST /payments\n@enduml")
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SearchNoPlantUml.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);
        var searchValues = ExtractDataSearchValues(content);

        var scenarioSearch = searchValues.FirstOrDefault(v => v.Contains("create order"));
        Assert.NotNull(scenarioSearch);
        Assert.DoesNotContain("@startuml", scenarioSearch);
        Assert.DoesNotContain("paymentgateway", scenarioSearch);
        Assert.DoesNotContain("orderservice", scenarioSearch);
    }

    [Fact]
    public void Data_search_for_parameterized_group_does_not_contain_plantuml_source()
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
                            new ScenarioStep { Keyword = "Given", Text = "the account has funds", Status = ExecutionResult.Passed }
                        ]
                    },
                    new Scenario
                    {
                        Id = "s2", DisplayName = "Withdraw $500", Result = ExecutionResult.Passed,
                        OutlineId = "withdraw-cash",
                        ExampleValues = new Dictionary<string, string> { ["Amount"] = "$500" },
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the account has funds", Status = ExecutionResult.Passed }
                        ]
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nAccountService -> Ledger : debit\n@enduml"),
            new DefaultDiagramsFetcher.DiagramAsCode("s2", "", "@startuml\nAccountService -> Ledger : debit\n@enduml")
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SearchNoPlantUmlParam.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        // Check both data-search (group level) and data-row-search (row level)
        var searchValues = ExtractDataSearchValues(content);
        foreach (var sv in searchValues)
        {
            Assert.DoesNotContain("@startuml", sv);
            Assert.DoesNotContain("accountservice", sv);
            Assert.DoesNotContain("ledger", sv);
        }

        var rowSearchValues = Regex.Matches(content, @"data-row-search=""([^""]*)""")
            .Select(m => m.Groups[1].Value)
            .ToArray();
        foreach (var rv in rowSearchValues)
        {
            Assert.DoesNotContain("@startuml", rv);
            Assert.DoesNotContain("accountservice", rv);
            Assert.DoesNotContain("ledger", rv);
        }
    }

    [Fact]
    public void Report_contains_search_loading_infrastructure()
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
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "something", Status = ExecutionResult.Passed }]
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nA -> B\n@enduml")
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SearchLoading.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        // The search bar should start disabled
        Assert.Contains("id=\"searchbar\"", content);
        var searchBarIdx = content.IndexOf("id=\"searchbar\"");
        var searchBarRegion = content.Substring(Math.Max(0, searchBarIdx - 50), Math.Min(content.Length - searchBarIdx + 50, 400));
        Assert.Contains("disabled", searchBarRegion);

        // There should be a loading overlay element
        Assert.Contains("search-loading-overlay", content);

        // Placeholder text should be hidden while loading (disabled state)
        Assert.Contains("#searchbar:disabled::placeholder", content);

        // There should be JS that decompresses plantuml-z and enriches data-search
        Assert.Contains("data-plantuml-z", content);
        Assert.Contains("enrichSearchData", content);
    }

    [Fact]
    public void EnrichSearchData_uses_chunked_processing_to_avoid_blocking()
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
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "something", Status = ExecutionResult.Passed }]
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nA -> B\n@enduml")
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SearchChunked.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        // The enrichSearchData function must yield back to the browser between chunks
        // to prevent freezing on large reports (e.g. 8000+ diagrams in 100MB+ reports)
        var enrichIdx = content.IndexOf("function enrichSearchData");
        Assert.True(enrichIdx >= 0, "enrichSearchData function should exist");
        var enrichEnd = content.IndexOf("function onEnrichComplete", enrichIdx);
        var enrichBody = content[enrichIdx..enrichEnd];
        Assert.Contains("setTimeout", enrichBody);

        // Batches must wait for all Promises in the current batch to resolve before
        // starting the next batch — otherwise all decompressions run concurrently
        Assert.Contains("Promise.all", enrichBody);
    }

    [Fact]
    public void EnrichSearchData_accumulates_search_text_before_flushing_to_DOM()
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
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "something", Status = ExecutionResult.Passed }]
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DefaultDiagramsFetcher.DiagramAsCode("s1", "", "@startuml\nA -> B\n@enduml")
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, "SearchAccumulate.html", "Test", includeTestRunData: true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        var content = File.ReadAllText(path);

        // Search text should be accumulated in memory and flushed once per element,
        // not incrementally appended via setAttribute inside each decompress callback.
        // This avoids O(n^2) string concatenation on large reports.
        var enrichIdx = content.IndexOf("function enrichSearchData");
        var enrichEnd = content.IndexOf("function onEnrichComplete", enrichIdx);
        var enrichBody = content[enrichIdx..enrichEnd];

        // The decompress .then() callback should NOT directly setAttribute('data-search', ...)
        // It should accumulate text in a JS object and flush later.
        var decompressIdx = enrichBody.IndexOf("decompress(");
        var promiseAllIdx = enrichBody.IndexOf("Promise.all");
        Assert.True(decompressIdx >= 0 && promiseAllIdx > decompressIdx, "decompress must appear before Promise.all");
        var decompressCallbackBody = enrichBody[decompressIdx..promiseAllIdx];
        Assert.DoesNotContain("setAttribute('data-search'", decompressCallbackBody);
        Assert.DoesNotContain("setAttribute('data-row-search'", decompressCallbackBody);
    }
}
