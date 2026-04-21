using Azure.Storage.Queues;

namespace TestTrackingDiagrams.Extensions.StorageQueues;

public static class QueueClientOptionsExtensions
{
    public static QueueClientOptions WithTestTracking(
        this QueueClientOptions options,
        StorageQueueTrackingMessageHandlerOptions trackingOptions,
        HttpMessageHandler? innerHandler = null)
    {
        var handler = new StorageQueueTrackingMessageHandler(trackingOptions, innerHandler ?? new HttpClientHandler());
        var httpClient = new HttpClient(handler);
        options.Transport = new Azure.Core.Pipeline.HttpClientTransport(httpClient);
        return options;
    }
}
