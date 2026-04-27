using Amazon.DynamoDBv2;

namespace TestTrackingDiagrams.Extensions.DynamoDB;

public static class AmazonDynamoDBConfigExtensions
{
    public static AmazonDynamoDBConfig WithTestTracking(
        this AmazonDynamoDBConfig config,
        DynamoDbTrackingMessageHandlerOptions trackingOptions,
        HttpMessageHandler? innerHandler = null)
    {
        var handler = new DynamoDbTrackingMessageHandler(trackingOptions, innerHandler ?? new HttpClientHandler(), trackingOptions.HttpContextAccessor);
        config.HttpClientFactory = new TrackingHttpClientFactory(handler);
        return config;
    }
}

internal class TrackingHttpClientFactory : Amazon.Runtime.HttpClientFactory
{
    private readonly HttpMessageHandler _handler;

    public TrackingHttpClientFactory(HttpMessageHandler handler)
    {
        _handler = handler;
    }

    public override HttpClient CreateHttpClient(Amazon.Runtime.IClientConfig clientConfig)
    {
        return new HttpClient(_handler);
    }
}
