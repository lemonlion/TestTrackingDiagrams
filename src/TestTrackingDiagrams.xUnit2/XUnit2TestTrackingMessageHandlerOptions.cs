using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.xUnit2;

public record XUnit2TestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public XUnit2TestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () =>
        {
            var (name, id) = XUnit2TestTrackingContext.GetCurrentTestInfo();
            return (name, id);
        };
    }
}
