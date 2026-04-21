using Amazon.DynamoDBv2;
using TestTrackingDiagrams.Extensions.DynamoDB;

namespace TestTrackingDiagrams.Tests.DynamoDB;

public class AmazonDynamoDBConfigExtensionsTests
{
    [Fact]
    public void WithTestTracking_ReturnsSameConfigInstance()
    {
        var config = new AmazonDynamoDBConfig();
        var options = new DynamoDbTrackingMessageHandlerOptions();

        var result = config.WithTestTracking(options);

        Assert.Same(config, result);
    }

    [Fact]
    public void WithTestTracking_SetsHttpClientFactory()
    {
        var config = new AmazonDynamoDBConfig();
        var options = new DynamoDbTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        Assert.NotNull(config.HttpClientFactory);
    }

    [Fact]
    public void WithTestTracking_FactoryCreatesHttpClient()
    {
        var config = new AmazonDynamoDBConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 };
        var options = new DynamoDbTrackingMessageHandlerOptions();

        config.WithTestTracking(options);

        var client = config.HttpClientFactory!.CreateHttpClient(config);
        Assert.NotNull(client);
    }
}
