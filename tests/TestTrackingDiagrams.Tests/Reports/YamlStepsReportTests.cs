using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class YamlStepsReportTests
{
    [Fact]
    public void Yaml_includes_steps_when_present()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "OrderService",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "Place order",
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "a valid request" },
                            new ScenarioStep { Keyword = "When", Text = "the order is placed" },
                            new ScenarioStep { Keyword = "Then", Text = "the order is created" }
                        ]
                    }
                ]
            }
        };

        var path = ReportGenerator.GenerateYamlSpecs([], features, "YamlSteps.yml", "Test");
        var content = File.ReadAllText(path);

        Assert.Contains("Steps:", content);
        Assert.Contains("Given a valid request", content);
        Assert.Contains("When the order is placed", content);
        Assert.Contains("Then the order is created", content);
    }

    [Fact]
    public void Yaml_omits_steps_when_none()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S" }]
            }
        };

        var path = ReportGenerator.GenerateYamlSpecs([], features, "YamlNoSteps.yml", "Test");
        var content = File.ReadAllText(path);

        Assert.DoesNotContain("Steps:", content);
    }

    [Fact]
    public void Yaml_includes_feature_description()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Description = "As a user I want to order cakes",
                Scenarios = [new Scenario { Id = "s1", DisplayName = "S" }]
            }
        };

        var path = ReportGenerator.GenerateYamlSpecs([], features, "YamlDesc.yml", "Test");
        var content = File.ReadAllText(path);

        Assert.Contains("Description:", content);
        Assert.Contains("As a user I want to order cakes", content);
    }

    [Fact]
    public void Yaml_includes_labels_and_categories()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Labels = ["api"],
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "S",
                        Labels = ["smoke"],
                        Categories = ["Ordering"]
                    }
                ]
            }
        };

        var path = ReportGenerator.GenerateYamlSpecs([], features, "YamlLabels.yml", "Test");
        var content = File.ReadAllText(path);

        Assert.Contains("Labels:", content);
        Assert.Contains("api", content);
        Assert.Contains("smoke", content);
        Assert.Contains("Categories:", content);
        Assert.Contains("Ordering", content);
    }
}
