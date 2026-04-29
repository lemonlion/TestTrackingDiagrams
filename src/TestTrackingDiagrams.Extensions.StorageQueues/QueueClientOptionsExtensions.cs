using Azure.Storage.Queues;

namespace TestTrackingDiagrams.Extensions.StorageQueues;

/// <summary>
/// Provides extension methods for configuring Azure Storage Queues client options to enable test tracking.
/// </summary>
public static class QueueClientOptionsExtensions
{
    public static QueueClientOptions WithTestTracking(
        this QueueClientOptions options,
        StorageQueueTrackingMessageHandlerOptions trackingOptions,
        HttpMessageHandler? innerHandler = null)
    {
        var handler = new StorageQueueTrackingMessageHandler(trackingOptions, innerHandler ?? new HttpClientHandler(), trackingOptions.HttpContextAccessor);
        var httpClient = new HttpClient(handler);
        options.Transport = new Azure.Core.Pipeline.HttpClientTransport(httpClient);
        return options;
    }
}