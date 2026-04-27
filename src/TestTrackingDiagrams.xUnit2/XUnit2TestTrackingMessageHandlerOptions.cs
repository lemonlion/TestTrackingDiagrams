using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.xUnit2;

public record XUnit2TestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current xUnit v2 test's display name and unique ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = CurrentTestInfo.Fetcher;

    public XUnit2TestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = CurrentTestInfo.Fetcher;
    }
}
