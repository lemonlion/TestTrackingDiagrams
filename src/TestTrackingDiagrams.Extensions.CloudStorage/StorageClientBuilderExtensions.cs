using Google.Cloud.Storage.V1;

namespace TestTrackingDiagrams.Extensions.CloudStorage;

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
