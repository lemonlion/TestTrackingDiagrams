using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for TUnit.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current TUnit test's display name and ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () =>
        {
            var ctx = TestContext.Current;
            return ctx is not null
                ? (ctx.Metadata.DisplayName, ctx.Id)
                : ("Unknown", "unknown");
        };
}
