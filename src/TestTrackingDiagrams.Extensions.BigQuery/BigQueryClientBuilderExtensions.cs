using Google.Cloud.BigQuery.V2;

namespace TestTrackingDiagrams.Extensions.BigQuery;

public static class BigQueryClientBuilderExtensions
{
    /// <summary>
    /// Configures a <see cref="BigQueryClientBuilder"/> to use a tracking HTTP handler that
    /// captures all BigQuery REST API operations for test diagrams.
    /// </summary>
    public static BigQueryClientBuilder WithTestTracking(
        this BigQueryClientBuilder builder,
        BigQueryTrackingMessageHandlerOptions trackingOptions)
    {
        builder.HttpClientFactory = new TrackingBigQueryHttpClientFactory(trackingOptions);
        return builder;
    }
}
