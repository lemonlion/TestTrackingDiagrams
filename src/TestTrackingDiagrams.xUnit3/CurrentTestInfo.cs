using Xunit;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for xUnit v3.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current xUnit v3 test's display name and unique ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () => (TestContext.Current.Test!.TestDisplayName, TestContext.Current.Test.UniqueID);
}
