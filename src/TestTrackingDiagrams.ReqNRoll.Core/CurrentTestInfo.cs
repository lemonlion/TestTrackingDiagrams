namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for ReqNRoll.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current ReqNRoll scenario's test name and ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () => ReqNRollTestContext.CurrentTestInfo
            ?? ("Unknown", "unknown");
}
