using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.MSTest;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.MSTest;

[TestClass]
public class MSTestTestTrackingMessageHandlerOptionsTests
{
    [TestMethod]
    public void ShouldHaveCurrentTestInfoFetcherSet()
    {
        var options = new MSTestTestTrackingMessageHandlerOptions();

        Assert.IsNotNull(options.CurrentTestInfoFetcher);
    }

    [TestMethod]
    public void ShouldInheritFromTestTrackingMessageHandlerOptions()
    {
        var options = new MSTestTestTrackingMessageHandlerOptions();

        Assert.IsInstanceOfType<TestTrackingMessageHandlerOptions>(options);
    }

    [TestMethod]
    public void ShouldAllowSettingCallingServiceName()
    {
        var options = new MSTestTestTrackingMessageHandlerOptions
        {
            CallingServiceName = "My API"
        };

        Assert.AreEqual("My API", options.CallingServiceName);
    }

    [TestMethod]
    public void ShouldAllowSettingPortsToServiceNames()
    {
        var options = new MSTestTestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = { { 80, "My API" }, { 5001, "Downstream" } }
        };

        Assert.AreEqual("My API", options.PortsToServiceNames[80]);
        Assert.AreEqual("Downstream", options.PortsToServiceNames[5001]);
    }

    [TestMethod]
    public void ShouldAllowSettingFixedNameForReceivingService()
    {
        var options = new MSTestTestTrackingMessageHandlerOptions
        {
            FixedNameForReceivingService = "My Service"
        };

        Assert.AreEqual("My Service", options.FixedNameForReceivingService);
    }
}
