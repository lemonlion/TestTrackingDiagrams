using TestTrackingDiagrams.Tracking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

public record MSTestTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public MSTestTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () =>
        {
            var ctx = DiagrammedComponentTest.GetCurrentTestContext();
            return (ctx!.TestName!, $"{ctx.FullyQualifiedTestClassName}.{ctx.TestName}");
        };
    }
}
