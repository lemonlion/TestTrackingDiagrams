using TestTrackingDiagrams.Tracking;
using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit4;

public record NUnitTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public NUnitTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (TestContext.CurrentContext.Test!.DisplayName!, TestContext.CurrentContext.Test.ID);
    }
}