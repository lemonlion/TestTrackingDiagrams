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
    public void SafeFetcher_Is_Not_Null()
    {
        Assert.NotNull(CurrentTestInfo.SafeFetcher);
    }

    [Fact]
    public void SafeFetcher_Returns_Fallback_When_No_TestContext()
    {
        // SafeFetcher should not throw even when TestContext.Current is null
        // (which it is during unit test execution outside of TUnit runner)
        var result = CurrentTestInfo.SafeFetcher();

        Assert.Equal("Unknown", result.Name);
        Assert.NotNull(result.Id);
        Assert.True(Guid.TryParse(result.Id, out _), "Fallback ID should be a valid GUID");
    }

    [Fact]
    public void SafeFetcher_Returns_Unique_Ids_Per_Invocation()
    {
        var result1 = CurrentTestInfo.SafeFetcher();
        var result2 = CurrentTestInfo.SafeFetcher();

        Assert.NotEqual(result1.Id, result2.Id);
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
