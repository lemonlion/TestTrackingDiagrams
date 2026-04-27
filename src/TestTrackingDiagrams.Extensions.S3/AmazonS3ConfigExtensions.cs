using Amazon.S3;

namespace TestTrackingDiagrams.Extensions.S3;

public static class AmazonS3ConfigExtensions
{
    public static AmazonS3Config WithTestTracking(
        this AmazonS3Config config,
        S3TrackingMessageHandlerOptions trackingOptions,
        HttpMessageHandler? innerHandler = null)
    {
        var handler = new S3TrackingMessageHandler(trackingOptions, innerHandler ?? new HttpClientHandler(), trackingOptions.HttpContextAccessor);
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
