namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for xUnit v2.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current xUnit v2 test's display name and unique ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () =>
        {
            var (name, id) = XUnit2TestTrackingContext.GetCurrentTestInfo();
            if (string.Equals(id, "unknown", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Test context not available on this thread.");
            return (name, id);
        };
}
