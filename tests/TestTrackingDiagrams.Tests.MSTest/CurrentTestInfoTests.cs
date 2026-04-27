using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.MSTest;

namespace TestTrackingDiagrams.Tests.MSTest;

[TestClass]
public class CurrentTestInfoTests
{
    [TestMethod]
    public void Fetcher_Is_Not_Null()
    {
        Assert.IsNotNull(CurrentTestInfo.Fetcher);
    }

    [TestMethod]
    public void Fetcher_Is_Same_Instance_As_Options_TestInfoFetcher()
    {
        Assert.AreSame(CurrentTestInfo.Fetcher, MSTestTestTrackingMessageHandlerOptions.TestInfoFetcher);
    }

    [TestMethod]
    public void Options_Constructor_Uses_Fetcher()
    {
        var options = new MSTestTestTrackingMessageHandlerOptions();

        Assert.AreSame(CurrentTestInfo.Fetcher, options.CurrentTestInfoFetcher);
    }
}
