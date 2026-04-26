namespace TestTrackingDiagrams.Tests.xUnit3.xUnit3;

using TestTrackingDiagrams.xUnit3;

public class XUnitTestTrackingMessageHandlerOptionsTests
{
    [Fact]
    public void TestInfoFetcher_Is_Static_And_Returns_Current_Test_Info()
    {
        var fetcher = XUnitTestTrackingMessageHandlerOptions.TestInfoFetcher;

        var (name, id) = fetcher();

        Assert.NotNull(name);
        Assert.NotEmpty(name);
        Assert.NotNull(id);
        Assert.NotEmpty(id);
    }

    [Fact]
    public void Constructor_Uses_Same_Fetcher_As_Static_Property()
    {
        var options = new XUnitTestTrackingMessageHandlerOptions();

        Assert.Same(XUnitTestTrackingMessageHandlerOptions.TestInfoFetcher, options.CurrentTestInfoFetcher);
    }
}
