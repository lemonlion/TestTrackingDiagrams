using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;

/// <summary>
/// xUnit v3-specific configuration options for the test tracking message handler.
/// </summary>
public record XUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current xUnit v3 test's display name and unique ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = CurrentTestInfo.Fetcher;

    public XUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = CurrentTestInfo.Fetcher;
    }
}