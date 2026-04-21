using Amazon.SimpleNotificationService;
using TestTrackingDiagrams.Extensions.SNS;

namespace TestTrackingDiagrams.Tests.SNS;

public class AmazonSNSConfigExtensionsTests
{
    [Fact]
    public void WithTestTracking_ReturnsSameConfig()
    {
        var config = new AmazonSimpleNotificationServiceConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new SnsTrackingMessageHandlerOptions();

        var result = config.WithTestTracking(options);

        Assert.Same(config, result);
    }

    [Fact]
    public void WithTestTracking_SetsHttpClientFactory()
    {
        var config = new AmazonSimpleNotificationServiceConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new SnsTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        Assert.NotNull(config.HttpClientFactory);
    }

    [Fact]
    public void WithTestTracking_FactoryCreatesHttpClient()
    {
        var config = new AmazonSimpleNotificationServiceConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new SnsTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        var client = config.HttpClientFactory!.CreateHttpClient(config);
        Assert.NotNull(client);
    }
}
