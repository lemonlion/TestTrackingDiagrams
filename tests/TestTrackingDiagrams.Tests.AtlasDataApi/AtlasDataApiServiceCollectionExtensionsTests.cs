using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.AtlasDataApi;

namespace TestTrackingDiagrams.Tests.AtlasDataApi;

public class AtlasDataApiServiceCollectionExtensionsTests
{
    [Fact]
    public void AddAtlasDataApiTestTracking_RegistersHandler()
    {
        var services = new ServiceCollection();

        services.AddAtlasDataApiTestTracking();

        var provider = services.BuildServiceProvider();
        var handler = provider.GetService<AtlasDataApiTrackingMessageHandler>();
        Assert.NotNull(handler);
    }

    [Fact]
    public void AddAtlasDataApiTestTracking_RegistersOptions()
    {
        var services = new ServiceCollection();

        services.AddAtlasDataApiTestTracking(o => o.ServiceName = "Custom");

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<AtlasDataApiTrackingMessageHandlerOptions>();
        Assert.Equal("Custom", options.ServiceName);
    }

    [Fact]
    public void AddAtlasDataApiTestTracking_RegistersAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddAtlasDataApiTestTracking();

        var descriptor = services.Single(s => s.ServiceType == typeof(AtlasDataApiTrackingMessageHandler));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddAtlasDataApiTestTracking_ReturnsServicesForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddAtlasDataApiTestTracking();

        Assert.Same(services, result);
    }
}
