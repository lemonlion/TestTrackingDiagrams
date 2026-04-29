using Amazon.SimpleNotificationService;

namespace TestTrackingDiagrams.Extensions.SNS;

/// <summary>
/// Provides extension methods for configuring Amazon SNS client options to enable test tracking.
/// </summary>
public static class AmazonSNSConfigExtensions
{
    public static AmazonSimpleNotificationServiceConfig WithTestTracking(
        this AmazonSimpleNotificationServiceConfig config,
        SnsTrackingMessageHandlerOptions options)
    {
        config.HttpClientFactory = new TrackingHttpClientFactory(
            new SnsTrackingMessageHandler(options, null, options.HttpContextAccessor));
        return config;
    }
}

internal class TrackingHttpClientFactory(HttpMessageHandler handler) : Amazon.Runtime.HttpClientFactory
{
    public override HttpClient CreateHttpClient(Amazon.Runtime.IClientConfig clientConfig) => new(handler);
}