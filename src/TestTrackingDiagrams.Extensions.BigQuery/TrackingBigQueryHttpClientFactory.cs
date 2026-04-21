using Google.Apis.Http;

namespace TestTrackingDiagrams.Extensions.BigQuery;

/// <summary>
/// An <see cref="Google.Apis.Http.HttpClientFactory"/> that inserts
/// a <see cref="BigQueryTrackingMessageHandler"/> into the HTTP pipeline
/// so that all BigQuery REST API calls are captured for test diagrams.
/// </summary>
internal class TrackingBigQueryHttpClientFactory : Google.Apis.Http.HttpClientFactory
{
    private readonly BigQueryTrackingMessageHandlerOptions _options;

    public TrackingBigQueryHttpClientFactory(BigQueryTrackingMessageHandlerOptions options)
    {
        _options = options;
    }

    protected override HttpMessageHandler CreateHandler(CreateHttpClientArgs args)
    {
        var innerHandler = base.CreateHandler(args);
        return new BigQueryTrackingMessageHandler(_options, innerHandler);
    }
}
