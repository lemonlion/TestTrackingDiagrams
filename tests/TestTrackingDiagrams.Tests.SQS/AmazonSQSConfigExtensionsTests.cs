using Amazon.SQS;
using TestTrackingDiagrams.Extensions.SQS;

namespace TestTrackingDiagrams.Tests.SQS;

public class AmazonSQSConfigExtensionsTests
{
    [Fact]
    public void WithTestTracking_ReturnsSameConfig()
    {
        var config = new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new SqsTrackingMessageHandlerOptions();

        var result = config.WithTestTracking(options);

        Assert.Same(config, result);
    }

    [Fact]
    public void WithTestTracking_SetsHttpClientFactory()
    {
        var config = new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new SqsTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        Assert.NotNull(config.HttpClientFactory);
    }

    [Fact]
    public void WithTestTracking_FactoryCreatesHttpClient()
    {
        var config = new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new SqsTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        var client = config.HttpClientFactory!.CreateHttpClient(config);
        Assert.NotNull(client);
    }
}
