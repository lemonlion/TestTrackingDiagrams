using Google.Cloud.Storage.V1;
using TestTrackingDiagrams.Extensions.CloudStorage;

namespace TestTrackingDiagrams.Tests.CloudStorage;

public class StorageClientBuilderExtensionsTests
{
    [Fact]
    public void WithTestTracking_Returns_Same_Builder_Instance()
    {
        var builder = new StorageClientBuilder();
        var options = new CloudStorageTrackingMessageHandlerOptions();

        var result = builder.WithTestTracking(options);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithTestTracking_Sets_HttpClientFactory()
    {
        var builder = new StorageClientBuilder();
        Assert.Null(builder.HttpClientFactory);

        builder.WithTestTracking(new CloudStorageTrackingMessageHandlerOptions());

        Assert.NotNull(builder.HttpClientFactory);
    }
}
