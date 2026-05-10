using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class BackgroundStepsDetectorTests
{
    private static Scenario MakeScenario(string id, params (string Keyword, string Text)[] steps) =>
        MakeScenario(id, null, steps);

    private static Scenario MakeScenario(string id, string? rule, params (string Keyword, string Text)[] steps) =>
        new()
        {
            Id = id,
            DisplayName = $"Scenario {id}",
            Result = ExecutionResult.Passed,
            Rule = rule,
            Steps = steps.Select(s => new ScenarioStep
            {
                Keyword = s.Keyword,
                Text = s.Text,
                Status = ExecutionResult.Passed
            }).ToArray()
        };

    // ── Basic prefix detection ──

    [Fact]
    public void Two_scenarios_sharing_common_prefix_extracts_background()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "the system is running"), ("Given", "a user exists"), ("When", "I do X"), ("Then", "Y happens")),
            MakeScenario("s2", ("Given", "the system is running"), ("Given", "a user exists"), ("When", "I do Z"), ("Then", "W happens"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.NotNull(scenarios[0].BackgroundSteps);
        Assert.Equal(2, scenarios[0].BackgroundSteps!.Length);
        Assert.Equal("the system is running", scenarios[0].BackgroundSteps[0].Text);
        Assert.Equal("a user exists", scenarios[0].BackgroundSteps[1].Text);

        // Steps should have the prefix removed
        Assert.Equal(2, scenarios[0].Steps!.Length);
        Assert.Equal("I do X", scenarios[0].Steps[0].Text);
        Assert.Equal("Y happens", scenarios[0].Steps[1].Text);

        // Both scenarios should get the same background
        Assert.NotNull(scenarios[1].BackgroundSteps);
        Assert.Equal(2, scenarios[1].BackgroundSteps!.Length);
        Assert.Equal(2, scenarios[1].Steps!.Length);
    }

    [Fact]
    public void No_common_prefix_does_not_extract_background()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "A"), ("When", "B")),
            MakeScenario("s2", ("Given", "X"), ("When", "Y"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.Null(scenarios[0].BackgroundSteps);
        Assert.Null(scenarios[1].BackgroundSteps);
        Assert.Equal(2, scenarios[0].Steps!.Length);
    }

    [Fact]
    public void Single_scenario_does_not_extract_background()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "the system is running"), ("When", "I do X"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.Null(scenarios[0].BackgroundSteps);
        Assert.Equal(2, scenarios[0].Steps!.Length);
    }

    [Fact]
    public void Partial_prefix_extracts_only_matching_steps()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "A"), ("Given", "B"), ("When", "C")),
            MakeScenario("s2", ("Given", "A"), ("Given", "X"), ("When", "Y"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.NotNull(scenarios[0].BackgroundSteps);
        Assert.Single(scenarios[0].BackgroundSteps!);
        Assert.Equal("A", scenarios[0].BackgroundSteps![0].Text);
        Assert.Equal(2, scenarios[0].Steps!.Length);
        Assert.Equal("B", scenarios[0].Steps[0].Text);
    }

    [Fact]
    public void Same_text_different_keyword_is_not_matched()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "the system is running"), ("When", "B")),
            MakeScenario("s2", ("When", "the system is running"), ("Then", "C"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.Null(scenarios[0].BackgroundSteps);
    }

    [Fact]
    public void Unequal_step_counts_handles_shortest()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "A"), ("When", "B"), ("Then", "C")),
            MakeScenario("s2", ("Given", "A"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.NotNull(scenarios[0].BackgroundSteps);
        Assert.Single(scenarios[0].BackgroundSteps!);
        Assert.Equal("A", scenarios[0].BackgroundSteps![0].Text);

        // s1 should have remaining steps
        Assert.Equal(2, scenarios[0].Steps!.Length);

        // s2 should have empty steps (all were background)
        Assert.Empty(scenarios[1].Steps!);
    }

    [Fact]
    public void Steps_with_sub_steps_are_preserved_on_background()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", ("Given", "setup")),
            MakeScenario("s2", ("Given", "setup"))
        };

        // Add sub-steps to the first scenario's first step
        scenarios[0].Steps![0].SubSteps =
        [
            new ScenarioStep { Keyword = "And", Text = "sub1", Status = ExecutionResult.Passed },
            new ScenarioStep { Keyword = "And", Text = "sub2", Status = ExecutionResult.Passed }
        ];

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.NotNull(scenarios[0].BackgroundSteps);
        Assert.NotNull(scenarios[0].BackgroundSteps![0].SubSteps);
        Assert.Equal(2, scenarios[0].BackgroundSteps[0].SubSteps!.Length);
    }

    // ── Rule-scoped detection ──

    [Fact]
    public void Two_rules_with_different_backgrounds_get_separate_extraction()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Rule A", ("Given", "A setup"), ("When", "A does X")),
            MakeScenario("s2", "Rule A", ("Given", "A setup"), ("When", "A does Y")),
            MakeScenario("s3", "Rule B", ("Given", "B setup"), ("When", "B does X")),
            MakeScenario("s4", "Rule B", ("Given", "B setup"), ("When", "B does Y"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        // Rule A scenarios
        Assert.NotNull(scenarios[0].BackgroundSteps);
        Assert.Equal("A setup", scenarios[0].BackgroundSteps![0].Text);
        Assert.Single(scenarios[0].Steps!);
        Assert.Equal("A does X", scenarios[0].Steps![0].Text);

        // Rule B scenarios
        Assert.NotNull(scenarios[2].BackgroundSteps);
        Assert.Equal("B setup", scenarios[2].BackgroundSteps![0].Text);
        Assert.Single(scenarios[2].Steps!);
        Assert.Equal("B does X", scenarios[2].Steps![0].Text);
    }

    [Fact]
    public void Rule_with_single_scenario_does_not_extract_background()
    {
        var scenarios = new[]
        {
            MakeScenario("s1", "Rule A", ("Given", "setup"), ("When", "X")),
            MakeScenario("s2", "Rule A", ("Given", "setup"), ("When", "Y")),
            MakeScenario("s3", "Rule B", ("Given", "setup"), ("When", "Z"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        // Rule A has 2 scenarios → should extract
        Assert.NotNull(scenarios[0].BackgroundSteps);

        // Rule B has 1 scenario → should NOT extract
        Assert.Null(scenarios[2].BackgroundSteps);
        Assert.Equal(2, scenarios[2].Steps!.Length);
    }

    [Fact]
    public void Mixed_scenarios_outside_and_inside_rules_detected_separately()
    {
        var scenarios = new[]
        {
            // Outside any rule
            MakeScenario("s1", null, ("Given", "global setup"), ("When", "X")),
            MakeScenario("s2", null, ("Given", "global setup"), ("When", "Y")),
            // Inside a rule
            MakeScenario("s3", "Rule A", ("Given", "rule setup"), ("When", "Z")),
            MakeScenario("s4", "Rule A", ("Given", "rule setup"), ("When", "W"))
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        // Outside-rule group
        Assert.NotNull(scenarios[0].BackgroundSteps);
        Assert.Equal("global setup", scenarios[0].BackgroundSteps![0].Text);

        // Rule A group
        Assert.NotNull(scenarios[2].BackgroundSteps);
        Assert.Equal("rule setup", scenarios[2].BackgroundSteps![0].Text);
    }

    [Fact]
    public void Empty_scenarios_array_does_not_throw()
    {
        BackgroundStepsDetector.DetectAndExtract([]);
    }

    [Fact]
    public void Scenarios_with_null_steps_are_skipped()
    {
        var scenarios = new[]
        {
            new Scenario { Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed, Steps = null },
            new Scenario { Id = "s2", DisplayName = "S2", Result = ExecutionResult.Passed, Steps = null }
        };

        BackgroundStepsDetector.DetectAndExtract(scenarios);

        Assert.Null(scenarios[0].BackgroundSteps);
    }
}
