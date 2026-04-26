using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;

public record XUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current xUnit v3 test's display name and unique ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher =
        () => (TestContext.Current.Test!.TestDisplayName, TestContext.Current.Test.UniqueID);

    public XUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = TestInfoFetcher;
    }
}