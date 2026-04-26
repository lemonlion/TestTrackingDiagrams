using TestTrackingDiagrams.Tracking;
using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

public record TUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current TUnit test's display name and ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher =
        () => (TestContext.Current!.Metadata.DisplayName, TestContext.Current.Id);

    public TUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = TestInfoFetcher;
    }
}
