using Microsoft.Azure.Cosmos;
using TestTrackingDiagrams.Extensions.CosmosDB;

namespace TestTrackingDiagrams.Tests.CosmosDB;

public class CosmosClientOptionsExtensionsTests
{
    [Fact]
    public void WithTestTracking_SetsConnectionModeToGateway()
    {
        var options = new CosmosClientOptions { ConnectionMode = ConnectionMode.Direct };

        options.WithTestTracking(new CosmosTrackingMessageHandlerOptions());

        Assert.Equal(ConnectionMode.Gateway, options.ConnectionMode);
    }

    [Fact]
    public void WithTestTracking_SetsHttpClientFactory()
    {
        var options = new CosmosClientOptions();

        options.WithTestTracking(new CosmosTrackingMessageHandlerOptions());

        Assert.NotNull(options.HttpClientFactory);
    }

    [Fact]
    public void WithTestTracking_HttpClientFactory_ReturnsHttpClient()
    {
        var options = new CosmosClientOptions();
        options.WithTestTracking(new CosmosTrackingMessageHandlerOptions());

        var client = options.HttpClientFactory!();

        Assert.NotNull(client);
        client.Dispose();
    }

    [Fact]
    public void WithTestTrackingAndCustomSslValidation_SetsConnectionModeToGateway()
    {
        var options = new CosmosClientOptions();

        options.WithTestTrackingAndCustomSslValidation(new CosmosTrackingMessageHandlerOptions());

        Assert.Equal(ConnectionMode.Gateway, options.ConnectionMode);
    }

    [Fact]
    public void WithTestTrackingAndCustomSslValidation_SetsHttpClientFactory()
    {
        var options = new CosmosClientOptions();

        options.WithTestTrackingAndCustomSslValidation(new CosmosTrackingMessageHandlerOptions());

        Assert.NotNull(options.HttpClientFactory);
    }
}
