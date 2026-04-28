namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Extension method that attaches phase-specific <see cref="PhaseVariant"/> data
/// to a <see cref="RequestResponseLog"/> when the test phase is unknown at capture
/// time and verbosity overrides are configured.
/// </summary>
public static class PhaseVariantExtensions
{
    /// <summary>
    /// Computes and attaches <see cref="RequestResponseLog.SetupVariant"/> and
    /// <see cref="RequestResponseLog.ActionVariant"/> when the current phase is
    /// <see cref="TestPhase.Unknown"/> and at least one verbosity override is set.
    /// </summary>
    /// <typeparam name="TVerbosity">The extension-specific verbosity enum type.</typeparam>
    /// <param name="log">The log entry to attach variants to.</param>
    /// <param name="baseVerbosity">The base (default) verbosity level.</param>
    /// <param name="setupVerbosity">Optional setup-phase verbosity override.</param>
    /// <param name="actionVerbosity">Optional action-phase verbosity override.</param>
    /// <param name="variantBuilder">
    /// A delegate that builds a <see cref="PhaseVariant"/> for a given verbosity level.
    /// Called once for setup and once for action when conditions are met.
    /// </param>
    public static void AttachVariants<TVerbosity>(
        this RequestResponseLog log,
        TVerbosity baseVerbosity,
        TVerbosity? setupVerbosity,
        TVerbosity? actionVerbosity,
        Func<TVerbosity, PhaseVariant> variantBuilder)
        where TVerbosity : struct
    {
        if (TestPhaseContext.Current != TestPhase.Unknown) return;
        if (!setupVerbosity.HasValue && !actionVerbosity.HasValue) return;

        log.SetupVariant = variantBuilder(setupVerbosity ?? baseVerbosity);
        log.ActionVariant = variantBuilder(actionVerbosity ?? baseVerbosity);
    }

    /// <summary>
    /// Fluent version of <see cref="AttachVariants{TVerbosity}"/> that returns the log
    /// for inline chaining with <c>RequestResponseLogger.Log(...)</c>.
    /// </summary>
    public static RequestResponseLog WithVariants<TVerbosity>(
        this RequestResponseLog log,
        TVerbosity baseVerbosity,
        TVerbosity? setupVerbosity,
        TVerbosity? actionVerbosity,
        Func<TVerbosity, PhaseVariant> variantBuilder)
        where TVerbosity : struct
    {
        log.AttachVariants(baseVerbosity, setupVerbosity, actionVerbosity, variantBuilder);
        return log;
    }
}
