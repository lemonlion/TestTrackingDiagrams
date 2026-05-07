using TestTrackingDiagrams.Tracking;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Tracking;

public class StepCollectorTests
{
    [Fact]
    public void StartStep_and_CompleteStep_records_single_passed_step()
    {
        var testId = "test-1";
        StepCollector.StartStep(testId, "Given", "A user exists", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("A user exists", steps[0].Text);
        Assert.Equal(ExecutionResult.Passed, steps[0].Status);
        Assert.NotNull(steps[0].Duration);
    }

    [Fact]
    public void StartStep_and_CompleteStep_records_failed_step()
    {
        var testId = "test-2";
        StepCollector.StartStep(testId, "When", "The user logs in", null, null);
        StepCollector.CompleteStep(testId, passed: false, errorMessage: "Login failed");

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("When", steps[0].Keyword);
        Assert.Equal(ExecutionResult.Failed, steps[0].Status);
    }

    [Fact]
    public void Multiple_steps_recorded_in_order()
    {
        var testId = "test-3";
        StepCollector.StartStep(testId, "Given", "A user exists", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "When", "The user logs in", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "Then", "The user is logged in", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Equal(3, steps.Length);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("When", steps[1].Keyword);
        Assert.Equal("Then", steps[2].Keyword);
    }

    [Fact]
    public void Nested_steps_become_substeps()
    {
        var testId = "test-4";
        StepCollector.StartStep(testId, "Given", "A user exists", null, null);
        StepCollector.StartStep(testId, "Step", "Create user in DB", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.NotNull(steps[0].SubSteps);
        Assert.Single(steps[0].SubSteps!);
        Assert.Equal("Create user in DB", steps[0].SubSteps![0].Text);
    }

    [Fact]
    public void Step_with_null_keyword_has_no_keyword()
    {
        var testId = "test-5";
        StepCollector.StartStep(testId, null, "Do something", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Null(steps[0].Keyword);
        Assert.Equal("Do something", steps[0].Text);
    }

    [Fact]
    public void GetSteps_returns_empty_for_unknown_testId()
    {
        var steps = StepCollector.GetSteps("nonexistent");
        Assert.Empty(steps);
    }

    [Fact]
    public void ClearSteps_removes_all_steps_for_test()
    {
        var testId = "test-6";
        StepCollector.StartStep(testId, "Given", "Something", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        StepCollector.ClearSteps(testId);

        var steps = StepCollector.GetSteps(testId);
        Assert.Empty(steps);
    }

    [Fact]
    public void Step_with_parameters_records_them()
    {
        var testId = "test-7";
        var paramNames = new[] { "name", "age" };
        var paramValues = new object?[] { "John", 42 };
        StepCollector.StartStep(testId, "Given", "A user exists", paramNames, paramValues);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].Parameters);
        Assert.Equal(2, steps[0].Parameters!.Length);
        Assert.Equal("name", steps[0].Parameters![0].Name);
        Assert.Equal("John", steps[0].Parameters![0].InlineValue!.Value);
        Assert.Equal("age", steps[0].Parameters![1].Name);
        Assert.Equal("42", steps[0].Parameters![1].InlineValue!.Value);
    }

    [Fact]
    public void Keyword_sequencing_Given_And_And()
    {
        var testId = "test-8";
        StepCollector.StartStep(testId, "Given", "A user exists", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "Given", "The user is active", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "Given", "The user has a subscription", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Equal(3, steps.Length);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("And", steps[1].Keyword);
        Assert.Equal("And", steps[2].Keyword);
    }

    [Fact]
    public void Keyword_sequencing_resets_for_different_keyword_types()
    {
        var testId = "test-9";
        StepCollector.StartStep(testId, "Given", "First given", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "Given", "Second given", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "When", "First when", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "When", "Second when", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "Then", "First then", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("And", steps[1].Keyword);
        Assert.Equal("When", steps[2].Keyword);
        Assert.Equal("And", steps[3].Keyword);
        Assert.Equal("Then", steps[4].Keyword);
    }

    [Fact]
    public void Null_keyword_Step_does_not_affect_sequencing()
    {
        var testId = "test-10";
        StepCollector.StartStep(testId, "Given", "First given", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, null, "A plain step", null, null);
        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.StartStep(testId, "Given", "Second given", null, null);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Null(steps[1].Keyword);
        Assert.Equal("And", steps[2].Keyword); // Still "And" because previous Given was first
    }

    [Fact]
    public void StartStep_with_null_testId_is_noop()
    {
        // Should not throw
        StepCollector.StartStep(null, "Given", "Something", null, null);
        StepCollector.CompleteStep(null, passed: true);
    }

    [Fact]
    public void CompleteStep_with_no_active_step_is_noop()
    {
        // Should not throw
        StepCollector.CompleteStep("no-active-steps", passed: true);
    }

    [Fact]
    public void Step_duration_is_positive()
    {
        var testId = "test-11";
        StepCollector.StartStep(testId, "Given", "Something", null, null);
        // Small delay to ensure duration > 0
        Thread.Sleep(1);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.True(steps[0].Duration!.Value.Ticks > 0);
    }

    [Fact]
    public void AddAssertionSubStep_adds_to_active_step()
    {
        var testId = "test-12";
        StepCollector.StartStep(testId, "Then", "Verify result", null, null);
        StepCollector.AddAssertionSubStep(testId, "result.Should().Be(42)", passed: true);
        StepCollector.AddAssertionSubStep(testId, "result.Should().BePositive()", passed: true);
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].SubSteps);
        Assert.Equal(2, steps[0].SubSteps!.Length);
        Assert.Equal("\u2713 result.Should().Be(42)", steps[0].SubSteps![0].Text);
        Assert.Equal(ExecutionResult.Passed, steps[0].SubSteps![0].Status);
    }

    [Fact]
    public void AddAssertionSubStep_failed_recorded_correctly()
    {
        var testId = "test-13";
        StepCollector.StartStep(testId, "Then", "Verify result", null, null);
        StepCollector.AddAssertionSubStep(testId, "result.Should().Be(42)", passed: false);
        StepCollector.CompleteStep(testId, passed: false);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].SubSteps);
        Assert.Equal("\u2717 result.Should().Be(42)", steps[0].SubSteps![0].Text);
        Assert.Equal(ExecutionResult.Failed, steps[0].SubSteps![0].Status);
    }

    [Fact]
    public void HasActiveStep_returns_true_when_step_in_progress()
    {
        var testId = "test-14";
        Assert.False(StepCollector.HasActiveStep(testId));

        StepCollector.StartStep(testId, "Given", "Something", null, null);
        Assert.True(StepCollector.HasActiveStep(testId));

        StepCollector.CompleteStep(testId, passed: true);
        Assert.False(StepCollector.HasActiveStep(testId));
    }

    [Fact]
    public void Track_AssertionPassed_adds_sub_step_when_step_active()
    {
        var testId = $"step-integration-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        StepCollector.StartStep(testId, "Then", "The result is correct", null, null);
        Track.AssertionPassed("x == 42");
        StepCollector.CompleteStep(testId, passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].SubSteps);
        Assert.Single(steps[0].SubSteps!);
        Assert.Equal("\u2713 x == 42", steps[0].SubSteps![0].Text);
        Assert.Equal(ExecutionResult.Passed, steps[0].SubSteps![0].Status);
    }

    [Fact]
    public void Track_AssertionFailed_adds_failed_sub_step_when_step_active()
    {
        var testId = $"step-integration-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        StepCollector.StartStep(testId, "Then", "The result is correct", null, null);
        Track.AssertionFailed("x == 42", "Expected 42 but got 0");
        StepCollector.CompleteStep(testId, passed: false);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.NotNull(steps[0].SubSteps);
        Assert.Single(steps[0].SubSteps!);
        Assert.Equal("\u2717 x == 42", steps[0].SubSteps![0].Text);
        Assert.Equal(ExecutionResult.Failed, steps[0].SubSteps![0].Status);
    }

    [Fact]
    public void Track_AssertionPassed_does_not_add_sub_step_when_no_active_step()
    {
        var testId = $"step-integration-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        // No step started — assertions should NOT appear in StepCollector
        Track.AssertionPassed("y == 10");

        var steps = StepCollector.GetSteps(testId);
        Assert.Empty(steps);
    }

    [Fact]
    public void StartStep_with_Given_sets_phase_to_Setup()
    {
        var testId = $"phase-{Guid.NewGuid():N}";
        TestPhaseContext.Reset();

        StepCollector.StartStep(testId, "Given", "A precondition", null, null);
        Assert.Equal(TestPhase.Setup, TestPhaseContext.Current);

        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.ClearSteps(testId);
        TestPhaseContext.Reset();
    }

    [Fact]
    public void StartStep_with_When_sets_phase_to_Action()
    {
        var testId = $"phase-{Guid.NewGuid():N}";
        TestPhaseContext.Reset();

        StepCollector.StartStep(testId, "When", "An action occurs", null, null);
        Assert.Equal(TestPhase.Action, TestPhaseContext.Current);

        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.ClearSteps(testId);
        TestPhaseContext.Reset();
    }

    [Fact]
    public void StartStep_with_Then_keeps_phase_as_Action()
    {
        var testId = $"phase-{Guid.NewGuid():N}";
        TestPhaseContext.Current = TestPhase.Action;

        StepCollector.StartStep(testId, "Then", "The result is X", null, null);
        Assert.Equal(TestPhase.Action, TestPhaseContext.Current);

        StepCollector.CompleteStep(testId, passed: true);
        StepCollector.ClearSteps(testId);
        TestPhaseContext.Reset();
    }

    [Fact]
    public void StartStep_phase_transition_respects_WhenTriggersAction_option()
    {
        var testId = $"phase-{Guid.NewGuid():N}";
        TestPhaseContext.Reset();
        var original = StepCollector.Options;

        try
        {
            StepCollector.Options = new StepTrackingOptions { WhenTriggersAction = false };
            StepCollector.StartStep(testId, "When", "An action", null, null);
            // Should NOT change phase when option is disabled
            Assert.Equal(TestPhase.Unknown, TestPhaseContext.Current);
        }
        finally
        {
            StepCollector.Options = original;
            StepCollector.CompleteStep(testId, passed: true);
            StepCollector.ClearSteps(testId);
            TestPhaseContext.Reset();
        }
    }

    [Fact]
    public void StartStep_self_resolving_uses_TestIdentityScope()
    {
        var testId = $"self-resolve-{Guid.NewGuid():N}";
        using var scope = TestIdentityScope.Begin(testId, testId);

        // Call the overload without testId — it should resolve from ambient context
        StepCollector.StartStep("Given", "Something happens", null, null);
        StepCollector.CompleteStep(passed: true);

        var steps = StepCollector.GetSteps(testId);
        Assert.Single(steps);
        Assert.Equal("Given", steps[0].Keyword);
        Assert.Equal("Something happens", steps[0].Text);
    }
}
