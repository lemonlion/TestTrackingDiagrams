namespace TestTrackingDiagrams.Tests.BDDfy;

public class CurrentTestInfoTests
{
    [Fact]
    public void BDDfy_xUnit3_CurrentTestInfo_Fetcher_is_not_null()
    {
        var fetcher = TestTrackingDiagrams.BDDfy.xUnit3.CurrentTestInfo.Fetcher;

        Assert.NotNull(fetcher);
    }

    [Fact]
    public void BDDfy_xUnit3_CurrentTestInfo_Fetcher_returns_test_info()
    {
        var fetcher = TestTrackingDiagrams.BDDfy.xUnit3.CurrentTestInfo.Fetcher;

        var (name, id) = fetcher();

        Assert.NotEmpty(name);
        Assert.NotEmpty(id);
    }
}
