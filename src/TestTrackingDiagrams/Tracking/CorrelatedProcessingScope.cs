namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Helper that combines <see cref="TestCorrelationStore"/> lookup with
/// <see cref="TestIdentityScope.Begin"/>. Used by background processing decorators
/// to establish test identity before invoking the real handler.
/// <para>
/// Returns <c>null</c> if no correlation is found for the key (no-op — processing
/// continues without test attribution, which is correct for items not produced by tests).
/// </para>
/// </summary>
public static class CorrelatedProcessingScope
{
    /// <summary>
    /// Looks up the test identity for the given correlation key and, if found,
    /// establishes a <see cref="TestIdentityScope"/> for the current async context.
    /// </summary>
    /// <param name="correlationKey">The work-item key to resolve (e.g. from <see cref="CorrelationKeys"/>).</param>
    /// <returns>
    /// An <see cref="IDisposable"/> that restores the previous identity on dispose,
    /// or <c>null</c> if no correlation was found.
    /// </returns>
    public static IDisposable? Begin(string correlationKey)
    {
        var identity = TestCorrelationStore.Resolve(correlationKey);
        if (identity is null) return null;
        return TestIdentityScope.Begin(identity.Value.Name, identity.Value.Id);
    }
}
