using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.XUnit;

public record XUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public XUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (TestContext.Current.Test!.TestDisplayName, TestContext.Current.Test.UniqueID);
    }
}