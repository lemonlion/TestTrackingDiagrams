using Azure.Storage.Blobs;

namespace TestTrackingDiagrams.Extensions.BlobStorage;

/// <summary>
/// Provides extension methods for configuring Azure Blob Storage client options to enable test tracking.
/// </summary>
public static class BlobClientOptionsExtensions
{
    /// <summary>
    /// Configures a <see cref="BlobClientOptions"/> to use the tracking message handler
    /// so that all Blob Storage operations are captured in test diagrams.
    /// </summary>
    /// <remarks>
    /// The Azure SDK allows overriding the HTTP transport. This method sets
    /// <c>Transport</c> to an <see cref="Azure.Core.Pipeline.HttpClientTransport"/>
    /// backed by an <see cref="HttpClient"/> that wraps a <see cref="BlobTrackingMessageHandler"/>.
    /// </remarks>
    public static BlobClientOptions WithTestTracking(
        this BlobClientOptions options,
        BlobTrackingMessageHandlerOptions trackingOptions,
        HttpMessageHandler? innerHandler = null)
    {
        var handler = new BlobTrackingMessageHandler(trackingOptions, innerHandler ?? new HttpClientHandler(), trackingOptions.HttpContextAccessor);
        var httpClient = new HttpClient(handler);
        options.Transport = new Azure.Core.Pipeline.HttpClientTransport(httpClient);
        return options;
    }
}