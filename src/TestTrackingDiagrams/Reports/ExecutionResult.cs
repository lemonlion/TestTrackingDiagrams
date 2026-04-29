namespace TestTrackingDiagrams.Reports;

/// <summary>
/// The execution outcome of a test scenario.
/// </summary>
public enum ExecutionResult
{
    /// <summary>The scenario passed all assertions.</summary>
    Passed,

    /// <summary>The scenario failed due to an assertion or unhandled exception.</summary>
    Failed,

    /// <summary>The scenario was explicitly skipped (e.g. via <c>[Skip]</c> or <c>[Ignore]</c>).</summary>
    Skipped,

    /// <summary>The scenario was bypassed by the framework (e.g. inconclusive).</summary>
    Bypassed,

    /// <summary>The scenario was skipped because a prior scenario in the same group failed.</summary>
    SkippedAfterFailure
}