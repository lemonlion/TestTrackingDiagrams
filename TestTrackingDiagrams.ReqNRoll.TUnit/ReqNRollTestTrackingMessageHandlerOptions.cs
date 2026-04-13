using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ReqNRoll.TUnit;

public record ReqNRollTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public ReqNRollTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => ReqNRollTestContext.CurrentTestInfo
            ?? throw new InvalidOperationException("No ReqNRoll scenario is currently executing. Ensure ReqNRollTrackingHooks is registered as a [Binding].");
        CurrentStepTypeFetcher = () => ReqNRollTestContext.CurrentStepType;
    }
}

