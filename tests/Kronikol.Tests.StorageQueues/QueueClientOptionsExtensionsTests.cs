using Azure.Storage.Queues;
using Kronikol.Extensions.StorageQueues;

namespace Kronikol.Tests.StorageQueues;

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
