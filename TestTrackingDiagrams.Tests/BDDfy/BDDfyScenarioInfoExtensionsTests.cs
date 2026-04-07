using TestTrackingDiagrams.BDDfy.xUnit3;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.BDDfy;

public class BDDfyScenarioInfoExtensionsTests
{
    private static BDDfyScenarioInfo MakeScenario(
        string testId = "t1",
        string storyTitle = "Story",
        string scenarioTitle = "Scenario",
        string? storyDescription = null,
        string[]? tags = null,
        List<BDDfyStepInfo>? steps = null,
        TestStack.BDDfy.Result result = TestStack.BDDfy.Result.Passed,
        TimeSpan duration = default)
    {
        return new BDDfyScenarioInfo
        {
            TestId = testId,
            StoryTitle = storyTitle,
            StoryDescription = storyDescription,
            ScenarioTitle = scenarioTitle,
            Tags = tags ?? [],
            Steps = steps ?? [],
            Result = result,
            Duration = duration
        };
    }

    [Fact]
    public void ToFeatures_maps_steps_to_scenario_steps()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "a valid request"),
            new BDDfyStepInfo("When", "the request is sent"),
            new BDDfyStepInfo("Then", "the response is 200")
        ]);

        var features = new[] { info }.ToFeatures();
        var scenario = features[0].Scenarios[0];

        Assert.NotNull(scenario.Steps);
        Assert.Equal(3, scenario.Steps!.Length);
        Assert.Equal("Given", scenario.Steps[0].Keyword);
        Assert.Equal("a valid request", scenario.Steps[0].Text);
        Assert.Equal("When", scenario.Steps[1].Keyword);
        Assert.Equal("Then", scenario.Steps[2].Keyword);
    }

    [Fact]
    public void ToFeatures_leaves_steps_null_when_no_steps()
    {
        var info = MakeScenario(steps: []);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Steps);
    }

    [Fact]
    public void ToFeatures_maps_story_description_to_feature_description()
    {
        var info = MakeScenario(storyDescription: "As a user I want to order");
        var features = new[] { info }.ToFeatures();
        Assert.Equal("As a user I want to order", features[0].Description);
    }

    [Fact]
    public void ToFeatures_leaves_description_null_when_no_story_description()
    {
        var info = MakeScenario(storyDescription: null);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Description);
    }

    [Fact]
    public void ToFeatures_maps_tags_to_scenario_labels_excluding_special_tags()
    {
        var info = MakeScenario(tags: ["smoke", "happy-path", "endpoint:GET /api", "regression"]);
        var features = new[] { info }.ToFeatures();
        var scenario = features[0].Scenarios[0];

        Assert.NotNull(scenario.Labels);
        Assert.Contains("smoke", scenario.Labels!);
        Assert.Contains("regression", scenario.Labels!);
        Assert.DoesNotContain("happy-path", scenario.Labels!);
        Assert.DoesNotContain("endpoint:GET /api", scenario.Labels!);
    }

    [Fact]
    public void ToFeatures_leaves_labels_null_when_no_non_special_tags()
    {
        var info = MakeScenario(tags: ["happy-path"]);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Labels);
    }

    [Fact]
    public void ToFeatures_maps_duration_to_scenario_duration()
    {
        var info = MakeScenario(duration: TimeSpan.FromMilliseconds(1234));
        var features = new[] { info }.ToFeatures();
        Assert.Equal(TimeSpan.FromMilliseconds(1234), features[0].Scenarios[0].Duration);
    }
}
