using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.MSTest;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for MSTest.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current MSTest test's name and fully-qualified ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () =>
        {
            var ctx = DiagrammedComponentTest.GetCurrentTestContext();
            return ctx is not null
                ? (ctx.TestName!, $"{ctx.FullyQualifiedTestClassName}.{ctx.TestName}")
                : TestIdentityScope.UnknownIdentity;
        };
}
