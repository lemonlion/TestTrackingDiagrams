using TestTrackingDiagrams.Tracking;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

public record MSTestTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current MSTest test's name and fully-qualified ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = () =>
    {
        var ctx = DiagrammedComponentTest.GetCurrentTestContext();
        return (ctx!.TestName!, $"{ctx.FullyQualifiedTestClassName}.{ctx.TestName}");
    };

    public MSTestTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = TestInfoFetcher;
    }
}
