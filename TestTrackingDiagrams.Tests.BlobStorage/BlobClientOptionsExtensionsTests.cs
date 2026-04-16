using Azure.Storage.Blobs;
using TestTrackingDiagrams.Extensions.BlobStorage;

namespace TestTrackingDiagrams.Tests.BlobStorage;

public class BlobClientOptionsExtensionsTests
{
    [Fact]
    public void WithTestTracking_SetsTransport()
    {
        var options = new BlobClientOptions();

        options.WithTestTracking(new BlobTrackingMessageHandlerOptions());

        Assert.NotNull(options.Transport);
    }

    [Fact]
    public void WithTestTracking_ReturnsSameOptions()
    {
        var options = new BlobClientOptions();

        var result = options.WithTestTracking(new BlobTrackingMessageHandlerOptions());

        Assert.Same(options, result);
    }
}
