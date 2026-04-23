namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Ambient context that holds the current test phase (Setup or Action).
/// BDD framework adapters set this automatically; non-BDD tests can set it
/// manually via <see cref="DefaultTrackingDiagramOverride.StartSetup"/> and
/// <see cref="DefaultTrackingDiagramOverride.StartAction(string)"/>.
/// </summary>
public static class TestPhaseContext
{
    private static readonly AsyncLocal<TestPhase> CurrentPhase = new();

    /// <summary>Gets or sets the current test phase for the executing async context.</summary>
    public static TestPhase Current
    {
        get => CurrentPhase.Value;
        set => CurrentPhase.Value = value;
    }

    /// <summary>Resets the phase to <see cref="TestPhase.Unknown"/>.</summary>
    public static void Reset() => CurrentPhase.Value = TestPhase.Unknown;
}
