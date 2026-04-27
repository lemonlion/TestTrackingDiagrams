using TestTrackingDiagrams.Tracking;
using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

public record NUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current NUnit test's display name and ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = CurrentTestInfo.Fetcher;

    public NUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = CurrentTestInfo.Fetcher;
    }
}