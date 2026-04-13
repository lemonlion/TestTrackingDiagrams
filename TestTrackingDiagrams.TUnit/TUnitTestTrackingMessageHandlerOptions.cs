using TestTrackingDiagrams.Tracking;
using TUnit.Core;

namespace TestTrackingDiagrams.TUnit;

public record TUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public TUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (TestContext.Current!.Metadata.DisplayName, TestContext.Current.Id);
    }
}
