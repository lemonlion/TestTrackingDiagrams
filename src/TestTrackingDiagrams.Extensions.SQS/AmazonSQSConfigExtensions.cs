using Amazon.SQS;

namespace TestTrackingDiagrams.Extensions.SQS;

/// <summary>
/// Provides extension methods for configuring Amazon SQS client options to enable test tracking.
/// </summary>
public static class AmazonSQSConfigExtensions
{
    public static AmazonSQSConfig WithTestTracking(
        this AmazonSQSConfig config,
        SqsTrackingMessageHandlerOptions options)
    {
        config.HttpClientFactory = new TrackingHttpClientFactory(
            new SqsTrackingMessageHandler(options, null, options.HttpContextAccessor));
        return config;
    }
}

internal class TrackingHttpClientFactory(HttpMessageHandler handler) : Amazon.Runtime.HttpClientFactory
{
    public override HttpClient CreateHttpClient(Amazon.Runtime.IClientConfig clientConfig) => new(handler);
}