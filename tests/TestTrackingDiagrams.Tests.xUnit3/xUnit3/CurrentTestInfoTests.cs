namespace TestTrackingDiagrams.Tests.xUnit3.xUnit3;

using TestTrackingDiagrams.xUnit3;

public class CurrentTestInfoTests
{
    [Fact]
    public void Fetcher_Is_Not_Null()
    {
        Assert.NotNull(CurrentTestInfo.Fetcher);
    }

    [Fact]
    public void Fetcher_Returns_Current_Test_Info()
    {
        var (name, id) = CurrentTestInfo.Fetcher();

        Assert.NotNull(name);
        Assert.NotEmpty(name);
        Assert.NotNull(id);
        Assert.NotEmpty(id);
    }

    [Fact]
    public void Fetcher_Is_Same_Instance_As_Options_TestInfoFetcher()
    {
        Assert.Same(CurrentTestInfo.Fetcher, XUnitTestTrackingMessageHandlerOptions.TestInfoFetcher);
    }

    [Fact]
    public void Options_Constructor_Uses_Fetcher()
    {
        var options = new XUnitTestTrackingMessageHandlerOptions();

        Assert.Same(CurrentTestInfo.Fetcher, options.CurrentTestInfoFetcher);
    }
}
