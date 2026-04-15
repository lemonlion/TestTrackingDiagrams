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

    // ─── Step status mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_step_result_passed()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "something", TestStack.BDDfy.Result.Passed)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Passed, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_result_failed()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "something", TestStack.BDDfy.Result.Failed)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_result_inconclusive_to_skipped()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "something", TestStack.BDDfy.Result.Inconclusive)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_result_not_implemented_to_skipped()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "something", TestStack.BDDfy.Result.NotImplemented)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_not_executed_after_failure_to_skipped_after_failure()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "this fails", TestStack.BDDfy.Result.Failed),
            new BDDfyStepInfo("Then", "this was not executed", TestStack.BDDfy.Result.NotExecuted)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Steps![0].Status);
        Assert.Equal(ExecutionResult.SkippedAfterFailure, features[0].Scenarios[0].Steps![1].Status);
    }

    [Fact]
    public void ToFeatures_maps_not_executed_without_prior_failure_to_skipped()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "this passed", TestStack.BDDfy.Result.Passed),
            new BDDfyStepInfo("Then", "this was not executed", TestStack.BDDfy.Result.NotExecuted)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Steps![1].Status);
    }

    // ─── Error extraction ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_error_message_and_stack_trace()
    {
        var info = new BDDfyScenarioInfo
        {
            TestId = "t1",
            StoryTitle = "Story",
            ScenarioTitle = "Scenario",
            Tags = [],
            Steps = [],
            Result = TestStack.BDDfy.Result.Failed,
            ErrorMessage = "Expected 200 but got 500",
            ErrorStackTrace = "at MyTests.Test() in Test.cs:line 42"
        };
        var features = new[] { info }.ToFeatures();
        Assert.Equal("Expected 200 but got 500", features[0].Scenarios[0].ErrorMessage);
        Assert.Equal("at MyTests.Test() in Test.cs:line 42", features[0].Scenarios[0].ErrorStackTrace);
    }

    [Fact]
    public void ToFeatures_leaves_error_fields_null_when_passed()
    {
        var info = MakeScenario(result: TestStack.BDDfy.Result.Passed);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].ErrorMessage);
        Assert.Null(features[0].Scenarios[0].ErrorStackTrace);
    }

    // ─── Step duration ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_step_duration()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "something", Duration: TimeSpan.FromMilliseconds(150))
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(TimeSpan.FromMilliseconds(150), features[0].Scenarios[0].Steps![0].Duration);
    }

    [Fact]
    public void ToFeatures_leaves_step_duration_null_when_not_provided()
    {
        var info = MakeScenario(steps:
        [
            new BDDfyStepInfo("Given", "something")
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Steps![0].Duration);
    }

    // ─── Feature name titleization ────────────────────────────────

    [Theory]
    [InlineData("AlternativeEvidenceScenarios", "Alternative Evidence Scenarios")]
    [InlineData("OrderProcessing", "Order Processing")]
    [InlineData("Alternative Evidence Scenarios", "Alternative Evidence Scenarios")]
    [InlineData("my_feature_name", "My Feature Name")]
    public void ToFeatures_titleizes_feature_display_name(string storyTitle, string expected)
    {
        var info = MakeScenario(storyTitle: storyTitle);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(expected, features[0].DisplayName);
    }
}
