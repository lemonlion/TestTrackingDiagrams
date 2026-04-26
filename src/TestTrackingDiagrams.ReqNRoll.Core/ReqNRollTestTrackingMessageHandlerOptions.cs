using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ReqNRoll;

public record ReqNRollTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current ReqNRoll scenario's test name and ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = () => ReqNRollTestContext.CurrentTestInfo
        ?? throw new InvalidOperationException("No ReqNRoll scenario is currently executing. Ensure ReqNRollTrackingHooks is registered as a [Binding].");

    public ReqNRollTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = TestInfoFetcher;
        CurrentStepTypeFetcher = () => ReqNRollTestContext.CurrentStepType;
    }
}
