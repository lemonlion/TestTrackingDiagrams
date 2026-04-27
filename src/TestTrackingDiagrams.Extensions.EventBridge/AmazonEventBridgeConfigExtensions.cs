using Amazon.EventBridge;

namespace TestTrackingDiagrams.Extensions.EventBridge;

public static class AmazonEventBridgeConfigExtensions
{
    public static AmazonEventBridgeConfig WithTestTracking(
        this AmazonEventBridgeConfig config,
        EventBridgeTrackingMessageHandlerOptions options)
    {
        config.HttpClientFactory = new TrackingHttpClientFactory(
            new EventBridgeTrackingMessageHandler(options, null, options.HttpContextAccessor));
        return config;
    }
}

internal class TrackingHttpClientFactory(HttpMessageHandler handler) : Amazon.Runtime.HttpClientFactory
{
    public override HttpClient CreateHttpClient(Amazon.Runtime.IClientConfig clientConfig) => new(handler);
}
