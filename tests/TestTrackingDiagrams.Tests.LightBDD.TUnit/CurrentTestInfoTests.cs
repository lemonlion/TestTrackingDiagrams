using TestTrackingDiagrams.LightBDD;

namespace TestTrackingDiagrams.Tests.LightBDD.TUnit;

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
        Assert.Same(CurrentTestInfo.Fetcher, LightBddTestTrackingMessageHandlerOptions.TestInfoFetcher);
    }

    [Fact]
    public void Options_Constructor_Uses_Fetcher()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions();

        Assert.Same(CurrentTestInfo.Fetcher, options.CurrentTestInfoFetcher);
    }
}
