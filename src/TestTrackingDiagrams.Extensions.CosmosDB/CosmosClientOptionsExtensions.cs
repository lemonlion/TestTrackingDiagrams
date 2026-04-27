using Microsoft.Azure.Cosmos;

namespace TestTrackingDiagrams.Extensions.CosmosDB;

public static class CosmosClientOptionsExtensions
{
    /// <summary>
    /// Configures <see cref="CosmosClientOptions"/> to use a tracking HTTP handler that
    /// captures all CosmosDB operations for test diagrams.
    /// <para>
    /// This forces <see cref="CosmosClientOptions.ConnectionMode"/> to <see cref="ConnectionMode.Gateway"/>
    /// because Direct mode uses a custom TCP protocol that bypasses the HTTP pipeline.
    /// </para>
    /// </summary>
    public static CosmosClientOptions WithTestTracking(
        this CosmosClientOptions options,
        CosmosTrackingMessageHandlerOptions trackingOptions,
        HttpMessageHandler? innerHandler = null)
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.HttpClientFactory = () =>
        {
            var handler = new CosmosTrackingMessageHandler(
                trackingOptions,
                innerHandler ?? new HttpClientHandler(),
                trackingOptions.HttpContextAccessor);
            return new HttpClient(handler);
        };

        return options;
    }

    /// <summary>
    /// Configures <see cref="CosmosClientOptions"/> to use a tracking HTTP handler that
    /// also allows custom SSL validation (useful for local emulator with self-signed certs).
    /// </summary>
    public static CosmosClientOptions WithTestTrackingAndCustomSslValidation(
        this CosmosClientOptions options,
        CosmosTrackingMessageHandlerOptions trackingOptions)
    {
        return options.WithTestTracking(trackingOptions, new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });
    }
}
