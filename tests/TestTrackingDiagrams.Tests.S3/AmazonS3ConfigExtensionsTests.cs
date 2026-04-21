using Amazon.S3;
using TestTrackingDiagrams.Extensions.S3;

namespace TestTrackingDiagrams.Tests.S3;

public class AmazonS3ConfigExtensionsTests
{
    [Fact]
    public void WithTestTracking_ReturnsSameConfigInstance()
    {
        var config = new AmazonS3Config();
        var options = new S3TrackingMessageHandlerOptions();

        var result = config.WithTestTracking(options);

        Assert.Same(config, result);
    }

    [Fact]
    public void WithTestTracking_SetsHttpClientFactory()
    {
        var config = new AmazonS3Config();
        var options = new S3TrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        Assert.NotNull(config.HttpClientFactory);
    }

    [Fact]
    public void WithTestTracking_FactoryCreatesHttpClient()
    {
        var config = new AmazonS3Config { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new S3TrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        var client = config.HttpClientFactory!.CreateHttpClient(config);
        Assert.NotNull(client);
    }
}
