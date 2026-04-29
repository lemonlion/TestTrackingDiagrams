using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Reqnroll-specific configuration options for the test tracking message handler.
/// </summary>
public record ReqNRollTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current ReqNRoll scenario's test name and ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = CurrentTestInfo.Fetcher;

    public ReqNRollTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = CurrentTestInfo.Fetcher;
        CurrentStepTypeFetcher = () => ReqNRollTestContext.CurrentStepType;
    }
}