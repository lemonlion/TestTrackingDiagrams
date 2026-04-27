using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for NUnit 4.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current NUnit test's display name and ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () => (TestContext.CurrentContext.Test!.DisplayName!, TestContext.CurrentContext.Test.ID);
}
