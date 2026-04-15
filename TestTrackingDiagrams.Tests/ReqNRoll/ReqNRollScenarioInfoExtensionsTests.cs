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

    // ─── Step status mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_step_status_ok_to_passed()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "something", ScenarioExecutionStatus.OK)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Passed, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_status_test_error_to_failed()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "something", ScenarioExecutionStatus.TestError)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_status_binding_error_to_failed()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "something", ScenarioExecutionStatus.BindingError)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_status_undefined_step_to_skipped()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "something", ScenarioExecutionStatus.UndefinedStep)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Steps![0].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_status_skipped_after_failure_to_skipped_after_failure()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "this fails", ScenarioExecutionStatus.TestError),
            new ReqNRollStepInfo("Then", "this was skipped", ScenarioExecutionStatus.Skipped)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Failed, features[0].Scenarios[0].Steps![0].Status);
        Assert.Equal(ExecutionResult.SkippedAfterFailure, features[0].Scenarios[0].Steps![1].Status);
    }

    [Fact]
    public void ToFeatures_maps_step_status_skipped_without_prior_failure_to_skipped()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "this passed", ScenarioExecutionStatus.OK),
            new ReqNRollStepInfo("Then", "this was skipped", ScenarioExecutionStatus.Skipped)
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(ExecutionResult.Skipped, features[0].Scenarios[0].Steps![1].Status);
    }

    // ─── Step duration mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_step_duration()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "something", Duration: TimeSpan.FromMilliseconds(250))
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal(TimeSpan.FromMilliseconds(250), features[0].Scenarios[0].Steps![0].Duration);
    }

    [Fact]
    public void ToFeatures_leaves_step_duration_null_when_not_provided()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "something")
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Steps![0].Duration);
    }

    // ─── DocString mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_step_doc_string()
    {
        var info = MakeScenario(steps:
        [
            new ReqNRollStepInfo("Given", "a request body", DocString: """{"name":"test"}""")
        ]);
        var features = new[] { info }.ToFeatures();
        Assert.Equal("""{"name":"test"}""", features[0].Scenarios[0].Steps![0].DocString);
    }

    // ─── Categories mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_category_tags_to_scenario_categories()
    {
        var info = MakeScenario(
            scenarioTags: ["smoke", "category:Integration"],
            combinedTags: ["smoke", "category:Integration"]);
        var features = new[] { info }.ToFeatures();
        var scenario = features[0].Scenarios[0];

        Assert.NotNull(scenario.Categories);
        Assert.Equal(["Integration"], scenario.Categories!);
        Assert.DoesNotContain("category:Integration", scenario.Labels!);
    }

    [Fact]
    public void ToFeatures_leaves_categories_null_when_no_category_tags()
    {
        var info = MakeScenario(scenarioTags: ["smoke"]);
        var features = new[] { info }.ToFeatures();
        Assert.Null(features[0].Scenarios[0].Categories);
    }

    // ─── Rule mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_rule()
    {
        var info = new ReqNRollScenarioInfo
        {
            ScenarioId = "s1",
            ScenarioTitle = "Test",
            FeatureTitle = "Feature",
            ScenarioTags = [],
            CombinedTags = [],
            Steps = [],
            Rule = "Business Rule 1"
        };
        var features = new[] { info }.ToFeatures();
        Assert.Equal("Business Rule 1", features[0].Scenarios[0].Rule);
    }

    // ─── ExampleValues mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_example_values()
    {
        var info = new ReqNRollScenarioInfo
        {
            ScenarioId = "s1",
            ScenarioTitle = "Test with <amount>",
            FeatureTitle = "Feature",
            ScenarioTags = [],
            CombinedTags = [],
            Steps = [],
            ExampleValues = new Dictionary<string, string> { ["amount"] = "100", ["currency"] = "GBP" }
        };
        var features = new[] { info }.ToFeatures();
        Assert.NotNull(features[0].Scenarios[0].ExampleValues);
        Assert.Equal("100", features[0].Scenarios[0].ExampleValues!["amount"]);
        Assert.Equal("GBP", features[0].Scenarios[0].ExampleValues!["currency"]);
    }

    // ─── Feature labels mapping ────────────────────────────────

    [Fact]
    public void ToFeatures_maps_feature_level_labels_from_combined_tags()
    {
        var info = MakeScenario(
            scenarioTags: ["scenario-tag"],
            combinedTags: ["feature-api", "scenario-tag", "endpoint:GET /api"]);
        var features = new[] { info }.ToFeatures();

        Assert.NotNull(features[0].Labels);
        Assert.Contains("feature-api", features[0].Labels!);
        Assert.DoesNotContain("scenario-tag", features[0].Labels!);
        Assert.DoesNotContain("endpoint:GET /api", features[0].Labels!);
    }
}
