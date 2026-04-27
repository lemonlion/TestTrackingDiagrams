using Elastic.Clients.Elasticsearch;

namespace TestTrackingDiagrams.Extensions.Elasticsearch;

public static class ElasticsearchClientSettingsExtensions
{
    public static ElasticsearchClientSettings WithTestTracking(
        this ElasticsearchClientSettings settings,
        ElasticsearchTrackingOptions options)
    {
        var handler = new ElasticsearchTrackingCallbackHandler(options, options.HttpContextAccessor);

        return settings
            .DisableDirectStreaming()
            .OnRequestCompleted(details => handler.HandleApiCallDetails(details));
    }
}
