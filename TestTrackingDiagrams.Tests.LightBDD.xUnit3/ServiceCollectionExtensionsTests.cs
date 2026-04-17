using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.LightBDD.xUnit3;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void TrackDependenciesForDiagrams_ShouldReturnServiceCollection()
    {
        var services = new ServiceCollection();
        var options = new LightBddTestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = new Dictionary<int, string> { { 5001, "TestService" } },
            CallingServiceName = "Caller"
        };

        var result = services.TrackDependenciesForDiagrams(options);

        Assert.Same(services, result);
    }

    [Fact]
    public void TrackDependenciesForDiagrams_ShouldRegisterOptions()
    {
        var services = new ServiceCollection();
        var options = new LightBddTestTrackingMessageHandlerOptions
        {
            PortsToServiceNames = new Dictionary<int, string> { { 5001, "TestService" } },
            CallingServiceName = "Caller"
        };

        services.TrackDependenciesForDiagrams(options);

        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<TestTrackingMessageHandlerOptions>();

        Assert.NotNull(resolved);
        Assert.IsType<LightBddTestTrackingMessageHandlerOptions>(resolved);
    }

    [Fact]
    public void TrackDependenciesForDiagrams_ShouldAcceptLightBddOptions()
    {
        var services = new ServiceCollection();
        var options = new LightBddTestTrackingMessageHandlerOptions();

        // Should compile and not throw - proves the extension method accepts the LightBDD-specific options type
        services.TrackDependenciesForDiagrams(options);

        Assert.True(services.Count > 0);
    }
}
