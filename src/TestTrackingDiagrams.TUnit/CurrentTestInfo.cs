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
        () => (TestContext.Current!.Metadata.DisplayName, TestContext.Current.Id);
}
