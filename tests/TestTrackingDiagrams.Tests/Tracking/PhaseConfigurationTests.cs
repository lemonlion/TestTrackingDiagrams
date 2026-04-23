using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class PhaseConfigurationTests
{
    public PhaseConfigurationTests()
    {
        TestPhaseContext.Reset();
    }

    // ─── ShouldTrack ────────────────────────────────────────────

    [Fact]
    public void ShouldTrack_returns_true_when_phase_is_Unknown()
    {
        Assert.True(PhaseConfiguration.ShouldTrack(trackDuringSetup: false, trackDuringAction: false));
    }

    [Fact]
    public void ShouldTrack_returns_true_during_Setup_when_enabled()
    {
        TestPhaseContext.Current = TestPhase.Setup;

        Assert.True(PhaseConfiguration.ShouldTrack(trackDuringSetup: true, trackDuringAction: false));
    }

    [Fact]
    public void ShouldTrack_returns_false_during_Setup_when_disabled()
    {
        TestPhaseContext.Current = TestPhase.Setup;

        Assert.False(PhaseConfiguration.ShouldTrack(trackDuringSetup: false, trackDuringAction: true));
    }

    [Fact]
    public void ShouldTrack_returns_true_during_Action_when_enabled()
    {
        TestPhaseContext.Current = TestPhase.Action;

        Assert.True(PhaseConfiguration.ShouldTrack(trackDuringSetup: false, trackDuringAction: true));
    }

    [Fact]
    public void ShouldTrack_returns_false_during_Action_when_disabled()
    {
        TestPhaseContext.Current = TestPhase.Action;

        Assert.False(PhaseConfiguration.ShouldTrack(trackDuringSetup: true, trackDuringAction: false));
    }

    // ─── GetEffectiveVerbosity ──────────────────────────────────

    [Fact]
    public void GetEffectiveVerbosity_returns_default_when_phase_is_Unknown()
    {
        var result = PhaseConfiguration.GetEffectiveVerbosity(
            defaultVerbosity: TestVerbosity.Detailed,
            setupVerbosity: TestVerbosity.Raw,
            actionVerbosity: TestVerbosity.Summarised);

        Assert.Equal(TestVerbosity.Detailed, result);
    }

    [Fact]
    public void GetEffectiveVerbosity_returns_setup_override_during_Setup()
    {
        TestPhaseContext.Current = TestPhase.Setup;

        var result = PhaseConfiguration.GetEffectiveVerbosity(
            defaultVerbosity: TestVerbosity.Detailed,
            setupVerbosity: TestVerbosity.Raw,
            actionVerbosity: TestVerbosity.Summarised);

        Assert.Equal(TestVerbosity.Raw, result);
    }

    [Fact]
    public void GetEffectiveVerbosity_returns_action_override_during_Action()
    {
        TestPhaseContext.Current = TestPhase.Action;

        var result = PhaseConfiguration.GetEffectiveVerbosity(
            defaultVerbosity: TestVerbosity.Detailed,
            setupVerbosity: TestVerbosity.Raw,
            actionVerbosity: TestVerbosity.Summarised);

        Assert.Equal(TestVerbosity.Summarised, result);
    }

    [Fact]
    public void GetEffectiveVerbosity_falls_back_to_default_when_setup_override_is_null()
    {
        TestPhaseContext.Current = TestPhase.Setup;

        var result = PhaseConfiguration.GetEffectiveVerbosity<TestVerbosity>(
            defaultVerbosity: TestVerbosity.Detailed,
            setupVerbosity: null,
            actionVerbosity: TestVerbosity.Summarised);

        Assert.Equal(TestVerbosity.Detailed, result);
    }

    [Fact]
    public void GetEffectiveVerbosity_falls_back_to_default_when_action_override_is_null()
    {
        TestPhaseContext.Current = TestPhase.Action;

        var result = PhaseConfiguration.GetEffectiveVerbosity<TestVerbosity>(
            defaultVerbosity: TestVerbosity.Detailed,
            setupVerbosity: TestVerbosity.Raw,
            actionVerbosity: null);

        Assert.Equal(TestVerbosity.Detailed, result);
    }

    // ─── ResolvePhaseFromStepType ───────────────────────────────

    [Theory]
    [InlineData("Given")]
    [InlineData("GIVEN")]
    [InlineData("given")]
    [InlineData("And")]
    [InlineData("AND")]
    [InlineData("But")]
    [InlineData("BUT")]
    public void ResolvePhaseFromStepType_returns_Setup_for_setup_keywords(string stepType)
    {
        Assert.Equal(TestPhase.Setup, PhaseConfiguration.ResolvePhaseFromStepType(stepType));
    }

    [Theory]
    [InlineData("When")]
    [InlineData("WHEN")]
    [InlineData("when")]
    [InlineData("Then")]
    [InlineData("THEN")]
    [InlineData("then")]
    public void ResolvePhaseFromStepType_returns_Action_for_action_keywords(string stepType)
    {
        Assert.Equal(TestPhase.Action, PhaseConfiguration.ResolvePhaseFromStepType(stepType));
    }

    [Fact]
    public void ResolvePhaseFromStepType_returns_Unknown_for_null()
    {
        Assert.Equal(TestPhase.Unknown, PhaseConfiguration.ResolvePhaseFromStepType(null));
    }

    [Fact]
    public void ResolvePhaseFromStepType_returns_Unknown_for_unrecognised_value()
    {
        Assert.Equal(TestPhase.Unknown, PhaseConfiguration.ResolvePhaseFromStepType("Arrange"));
    }

    // ─── Test helper enum ───────────────────────────────────────

    private enum TestVerbosity
    {
        Raw,
        Detailed,
        Summarised
    }
}
