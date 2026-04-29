using Google.Cloud.Storage.V1;

namespace TestTrackingDiagrams.Extensions.CloudStorage;

/// <summary>
/// Provides extension methods for configuring Google Cloud Storage client options to enable test tracking.
/// </summary>
public static class StorageClientBuilderExtensions
{
    public static StorageClientBuilder WithTestTracking(
        this StorageClientBuilder builder,
        CloudStorageTrackingMessageHandlerOptions options)
    {
        builder.HttpClientFactory = new TrackingCloudStorageHttpClientFactory(options);
        return builder;
    }
}