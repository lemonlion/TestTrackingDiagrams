using Reqnroll;
using TestTrackingDiagrams.ReqNRoll.xUnit3;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.ReqNRoll;

public class ReqNRollScenarioInfoExtensionsTests
{
    private static ReqNRollScenarioInfo MakeScenario(
        string scenarioId = "s1",
        string scenarioTitle = "Scenario",
        string featureTitle = "Feature",
        string? featureDescription = null,
        string[]? scenarioTags = null,
        string[]? combinedTags = null,
        List<ReqNRollStepInfo>? steps = null,
        ScenarioExecutionStatus status = ScenarioExecutionStatus.OK)
    {
        var tags = scenarioTags ?? [];
        return new ReqNRollScenarioInfo
        {
            ScenarioId = scenarioId,
            ScenarioTitle = scenarioTitle,
            FeatureTitle = featureTitle,
            FeatureDescription = featureDescription,
            ScenarioTags = tags,
            CombinedTags = combinedTags ?? tags,
            Steps = steps ?? [],
            ExecutionStatus = status
        };
    }

    [Fact]
    public void ToFeatures_maps_steps_to_scenario_steps()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "a valid request"),
            new ReqNRollStepInfo("When", "the request is sent"),
            new ReqNRollStepInfo("Then", "the response is 200")
        ]);

        var features = new[] { info }.ToFeatures();
        var scenario = features[0].Scenarios[0];

        Assert.NotNull(scenario.Steps);
        Assert.Equal(3, scenario.Steps!.Length);
        Assert.Equal("Given", scenario.Steps[0].Keyword);
        Assert.Equal("a valid request", scenario.Steps[0].Text);
    }

    [Fact]
    public void ToFeatures_leaves_steps_null_when_no_steps()
    {
        var info = MakeScenario(steps: []);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Steps);
    }

    [Fact]
    public void ToFeatures_maps_feature_description()
    {
        var info = MakeScenario(featureDescription: "As a user I want to register");
        var features = new[] { info }.ToFeatures();
        Assert.Equal("As a user I want to register", features[0].Description);
    }

    [Fact]
    public void ToFeatures_leaves_description_null_when_absent()
    {
        var info = MakeScenario(featureDescription: null);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Description);
    }

    [Fact]
    public void ToFeatures_maps_tags_to_scenario_labels_excluding_special_tags()
    {
        var info = MakeScenario(
            scenarioTags: ["smoke", "happy-path", "regression"],
            combinedTags: ["smoke", "happy-path", "endpoint:GET /api", "regression"]);

        var features = new[] { info }.ToFeatures();
        var scenario = features[0].Scenarios[0];

        Assert.NotNull(scenario.Labels);
        Assert.Contains("smoke", scenario.Labels!);
        Assert.Contains("regression", scenario.Labels!);
        Assert.DoesNotContain("happy-path", scenario.Labels!);
    }

    [Fact]
    public void ToFeatures_leaves_labels_null_when_only_special_tags()
    {
        var info = MakeScenario(scenarioTags: ["happy-path"]);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Labels);
    }
}
