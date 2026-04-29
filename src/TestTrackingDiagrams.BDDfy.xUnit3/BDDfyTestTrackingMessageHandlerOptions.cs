using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

/// <summary>
/// BDDfy-specific configuration options for the test tracking message handler.
/// </summary>
public record BDDfyTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current xUnit v3 test's display name and unique ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = CurrentTestInfo.Fetcher;

    public BDDfyTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = CurrentTestInfo.Fetcher;
        CurrentStepTypeFetcher = () => BDDfyStepTrackingExecutor.CurrentStepType;
    }
}