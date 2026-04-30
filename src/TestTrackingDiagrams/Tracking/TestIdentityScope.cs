namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Ambient <see cref="AsyncLocal{T}"/>-based scope that carries test identity into
/// background threads, hosted services, change-feed subscribers, and other code paths
/// where neither <c>HttpContext</c> nor the test framework's <c>TestContext</c> is available.
/// <para>
/// Resolution order in <see cref="TestInfoResolver"/>:
/// <list type="number">
///   <item>HTTP request headers (propagated by <see cref="TestTrackingMessageHandler"/>)</item>
///   <item><c>CurrentTestInfoFetcher</c> delegate (test framework AsyncLocal)</item>
///   <item><see cref="TestIdentityScope.Current"/> (this class)</item>
/// </list>
/// </para>
/// <example>
/// <code>
/// // Wrap background processing that is logically part of the test:
/// using (TestIdentityScope.Begin(testName, testId))
/// {
///     await backgroundService.ProcessAsync(); // tracking attributes to this test
/// }
/// </code>
/// </example>
/// </summary>
public static class TestIdentityScope
{
    private static readonly AsyncLocal<(string Name, string Id)?> CurrentIdentity = new();

    /// <summary>
    /// Gets the current test identity from the ambient scope, or <c>null</c> if no scope is active.
    /// </summary>
    public static (string Name, string Id)? Current => CurrentIdentity.Value;

    /// <summary>
    /// Sets the ambient test identity for the current async context.
    /// Returns an <see cref="IDisposable"/> that restores the previous value on dispose.
    /// </summary>
    /// <param name="testName">The test display name.</param>
    /// <param name="testId">The test unique identifier.</param>
    public static IDisposable Begin(string testName, string testId)
    {
        var previous = CurrentIdentity.Value;
        CurrentIdentity.Value = (testName, testId);
        return new IdentityScope(previous);
    }

    /// <summary>
    /// Clears the ambient test identity for the current async context.
    /// </summary>
    public static void Reset() => CurrentIdentity.Value = null;

    private sealed class IdentityScope((string Name, string Id)? previous) : IDisposable
    {
        public void Dispose() => CurrentIdentity.Value = previous;
    }
}
