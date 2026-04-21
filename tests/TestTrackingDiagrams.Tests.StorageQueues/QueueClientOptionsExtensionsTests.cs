using Azure.Storage.Queues;
using TestTrackingDiagrams.Extensions.StorageQueues;

namespace TestTrackingDiagrams.Tests.StorageQueues;

public class QueueClientOptionsExtensionsTests
{
    [Fact]
    public void WithTestTracking_SetsTransport()
    {
        var options = new QueueClientOptions();

        options.WithTestTracking(new StorageQueueTrackingMessageHandlerOptions());

        Assert.NotNull(options.Transport);
    }

    [Fact]
    public void WithTestTracking_ReturnsSameOptions()
    {
        var options = new QueueClientOptions();

        var result = options.WithTestTracking(new StorageQueueTrackingMessageHandlerOptions());

        Assert.Same(options, result);
    }
}
