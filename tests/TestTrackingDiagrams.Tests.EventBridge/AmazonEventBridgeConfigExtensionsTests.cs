using Amazon.EventBridge;
using TestTrackingDiagrams.Extensions.EventBridge;

namespace TestTrackingDiagrams.Tests.EventBridge;

public class AmazonEventBridgeConfigExtensionsTests
{
    [Fact]
    public void WithTestTracking_ReturnsSameConfig()
    {
        var config = new AmazonEventBridgeConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new EventBridgeTrackingMessageHandlerOptions();

        var result = config.WithTestTracking(options);

        Assert.Same(config, result);
    }

    [Fact]
    public void WithTestTracking_SetsHttpClientFactory()
    {
        var config = new AmazonEventBridgeConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new EventBridgeTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        Assert.NotNull(config.HttpClientFactory);
    }

    [Fact]
    public void WithTestTracking_FactoryCreatesHttpClient()
    {
        var config = new AmazonEventBridgeConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new EventBridgeTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        var client = config.HttpClientFactory!.CreateHttpClient(config);
        Assert.NotNull(client);
    }
}
