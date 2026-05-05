using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for TUnit.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current TUnit test's display name and ID.
    /// Throws <see cref="InvalidOperationException"/> if no test context is available.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () =>
        {
            var ctx = TestContext.Current
                ?? throw new InvalidOperationException("Test context not available on this thread.");
            return (ctx.Metadata.DisplayName, ctx.Id);
        };

}
