namespace TestTrackingDiagrams.Tests.xUnit2.xUnit2;

using TestTrackingDiagrams.xUnit2;

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
        Assert.Same(CurrentTestInfo.Fetcher, XUnit2TestTrackingMessageHandlerOptions.TestInfoFetcher);
    }

    [Fact]
    public void Options_Constructor_Uses_Fetcher()
    {
        var options = new XUnit2TestTrackingMessageHandlerOptions();

        Assert.Same(CurrentTestInfo.Fetcher, options.CurrentTestInfoFetcher);
    }
}
