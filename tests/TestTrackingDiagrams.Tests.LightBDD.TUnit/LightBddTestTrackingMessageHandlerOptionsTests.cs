using LightBDD.Core.ExecutionContext;
using LightBDD.Core.Metadata;
using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.LightBDD.TUnit;

public class LightBddTestTrackingMessageHandlerOptionsTests
{
    [Fact]
    public void ShouldInheritFromTestTrackingMessageHandlerOptions()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions();

        Assert.IsAssignableFrom<TestTrackingMessageHandlerOptions>(options);
    }

    [Fact]
    public void ShouldHaveCurrentTestInfoFetcherSet()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions();

        Assert.NotNull(options.CurrentTestInfoFetcher);
    }

    [Fact]
    public void ShouldHaveCurrentStepTypeFetcherSet()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions();

        Assert.NotNull(options.CurrentStepTypeFetcher);
    }

    [Fact]
    public void ShouldThrowWhenStepTypeFetcherCalledOutsideScenarioContext()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions();

        // Outside a LightBDD scenario, ScenarioExecutionContext throws
        Assert.Throws<InvalidOperationException>(() => options.CurrentStepTypeFetcher!());
    }

    [Fact]
    public void ShouldAllowSettingPortsToServiceNames()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = new Dictionary<int, string> { { 5001, "MyService" } }
        };

        Assert.Equal("MyService", options.PortsToServiceNames[5001]);
    }

    [Fact]
    public void ShouldAllowSettingCallerName()
    {
        var options = new LightBddTestTrackingMessageHandlerOptions
        {
            CallerName = "TestCaller"
        };

        Assert.Equal("TestCaller", options.CallerName);
    }
}
