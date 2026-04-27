using Google.Apis.Http;

namespace TestTrackingDiagrams.Extensions.CloudStorage;

internal class TrackingCloudStorageHttpClientFactory : Google.Apis.Http.HttpClientFactory
{
    private readonly CloudStorageTrackingMessageHandlerOptions _options;

    public TrackingCloudStorageHttpClientFactory(CloudStorageTrackingMessageHandlerOptions options)
    {
        _options = options;
    }

    protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args)
    {
        var innerHandler = base.CreateHandler(args);
        return new CloudStorageTrackingMessageHandler(_options, innerHandler, _options.HttpContextAccessor);
    }
}
