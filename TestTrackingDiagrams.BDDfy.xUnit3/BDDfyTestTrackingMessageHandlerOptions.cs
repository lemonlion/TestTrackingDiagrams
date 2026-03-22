using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public record BDDfyTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public BDDfyTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (Xunit.TestContext.Current.Test!.TestDisplayName, Xunit.TestContext.Current.Test.UniqueID);
    }
}
