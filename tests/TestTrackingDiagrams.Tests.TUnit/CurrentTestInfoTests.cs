using TestTrackingDiagrams.TUnit;

namespace TestTrackingDiagrams.Tests.TUnit;

public class CurrentTestInfoTests
{
    [Fact]
    public void Fetcher_Is_Not_Null()
    {
        Assert.NotNull(CurrentTestInfo.Fetcher);
    }

    [Fact]
    public void Fetcher_Is_Same_Instance_As_Options_TestInfoFetcher()
    {
        Assert.Same(CurrentTestInfo.Fetcher, TUnitTestTrackingMessageHandlerOptions.TestInfoFetcher);
    }

    [Fact]
    public void Options_Constructor_Uses_Fetcher()
    {
        var options = new TUnitTestTrackingMessageHandlerOptions();

        Assert.Same(CurrentTestInfo.Fetcher, options.CurrentTestInfoFetcher);
    }
}
