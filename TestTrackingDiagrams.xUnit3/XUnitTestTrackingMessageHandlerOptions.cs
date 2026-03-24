using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.xUnit3;

public record XUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public XUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (TestContext.Current.Test!.TestDisplayName, TestContext.Current.Test.UniqueID);
    }
}