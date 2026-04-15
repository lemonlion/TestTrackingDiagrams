using System.Text.Json;
using System.Xml.Linq;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class SpecificationsDataTests
{
    [Fact]
    public void GenerateSpecificationsData_produces_yaml_by_default()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Place order", IsHappyPath = true }
                ]
            }
        };

        var path = ReportGenerator.GenerateSpecificationsData(features, "SpecsData_yaml.yml", "Service Specifications", DataFormat.Yaml);
        var content = File.ReadAllText(path);

        Assert.Contains("Title: Service Specifications", content);
        Assert.Contains("Features:", content);
        Assert.Contains("Orders", content);
        Assert.Contains("Place order", content);
    }

    [Fact]
    public void GenerateSpecificationsData_produces_json()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Endpoint = "POST /api/orders",
                Description = "Order management",
                Labels = ["api"],
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Place order", IsHappyPath = true,
                        Labels = ["smoke"], Categories = ["Integration"],
                        Steps = [new ScenarioStep { Keyword = "Given", Text = "a request" }]
                    }
                ]
            }
        };

        var path = ReportGenerator.GenerateSpecificationsData(features, "SpecsData_json.json", "Service Specs", DataFormat.Json);
        var content = File.ReadAllText(path);

        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.Equal("Service Specs", root.GetProperty("title").GetString());

        var feature = root.GetProperty("features")[0];
        Assert.Equal("Orders", feature.GetProperty("name").GetString());
        Assert.Equal("POST /api/orders", feature.GetProperty("endpoint").GetString());
        Assert.Equal("Order management", feature.GetProperty("description").GetString());
        Assert.Equal("api", feature.GetProperty("labels")[0].GetString());

        var scenario = feature.GetProperty("scenarios")[0];
        Assert.Equal("Place order", scenario.GetProperty("name").GetString());
        Assert.True(scenario.GetProperty("isHappyPath").GetBoolean());
        Assert.Equal("smoke", scenario.GetProperty("labels")[0].GetString());
        Assert.Equal("Integration", scenario.GetProperty("categories")[0].GetString());

        var step = scenario.GetProperty("steps")[0];
        Assert.Equal("Given a request", step.GetString());
    }

    [Fact]
    public void GenerateSpecificationsData_produces_xml()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "Place order" }]
            }
        };

        var path = ReportGenerator.GenerateSpecificationsData(features, "SpecsData_xml.xml", "Specs", DataFormat.Xml);
        var content = File.ReadAllText(path);

        var doc = XDocument.Parse(content);
        Assert.Equal("Specifications", doc.Root!.Name.LocalName);
        Assert.Equal("Specs", doc.Root.Element("Title")!.Value);
        Assert.NotNull(doc.Root.Element("Features"));
    }

    [Fact]
    public void GenerateSpecificationsData_blank_on_failed_tests_when_configured()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Failed, ErrorMessage = "err" }]
            }
        };

        var path = ReportGenerator.GenerateSpecificationsData(features, "SpecsData_blank.json", "Specs", DataFormat.Json, generateBlankOnFailedTests: true);
        var content = File.ReadAllText(path);

        Assert.Equal(string.Empty, content);
    }

    [Fact]
    public void GenerateSpecificationsData_yaml_matches_existing_yaml_specs_format()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Endpoint = "POST /api/orders",
                Description = "Manage orders",
                Labels = ["api"],
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Place order", IsHappyPath = true,
                        Labels = ["smoke"], Categories = ["Ordering"],
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "a valid request" },
                            new ScenarioStep
                            {
                                Keyword = "When", Text = "the order is placed",
                                SubSteps = [new ScenarioStep { Keyword = "And", Text = "payment processed" }]
                            }
                        ]
                    }
                ]
            }
        };

        // YAML format should match the existing GenerateYamlSpecs output
        var existingPath = ReportGenerator.GenerateYamlSpecs([], features, "SpecsData_existing.yml", "Service Specs");
        var existingContent = File.ReadAllText(existingPath);

        var newPath = ReportGenerator.GenerateSpecificationsData(features, "SpecsData_new.yml", "Service Specs", DataFormat.Yaml);
        var newContent = File.ReadAllText(newPath);

        Assert.Equal(existingContent, newContent);
    }
}
