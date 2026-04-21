using Azure.Messaging.ServiceBus;
using TestTrackingDiagrams.Extensions.ServiceBus;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ServiceBus;

[Collection("TrackingComponentRegistry")]
public class TrackingServiceBusClientTests : IDisposable
{
    private const string FakeConnectionString =
        "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=key;SharedAccessKey=dGVzdA==";

    private readonly ServiceBusClient _realClient;

    public TrackingServiceBusClientTests()
    {
        _realClient = new ServiceBusClient(FakeConnectionString);
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        _realClient.DisposeAsync().AsTask().GetAwaiter().GetResult();
        TrackingComponentRegistry.Clear();
    }

    private static ServiceBusTrackingOptions MakeOptions() => new()
    {
        ServiceName = "TestBus",
        CallingServiceName = "TestCaller",
        CurrentTestInfoFetcher = () => ("Test", Guid.NewGuid().ToString()),
    };

    [Fact]
    public void Inner_ReturnsOriginalClient()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        Assert.Same(_realClient, tracked.Inner);
    }

    [Fact]
    public void FullyQualifiedNamespace_DelegatesToInner()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        Assert.Equal("test.servicebus.windows.net", tracked.FullyQualifiedNamespace);
    }

    [Fact]
    public void IsClosed_DelegatesToInner()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        Assert.False(tracked.IsClosed);
    }

    [Fact]
    public void CreateSender_ReturnsTrackingServiceBusSender()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        var sender = tracked.CreateSender("orders-queue");

        Assert.NotNull(sender);
        Assert.IsType<TrackingServiceBusSender>(sender);
    }

    [Fact]
    public void CreateSender_SenderHasCorrectEntityPath()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        var sender = tracked.CreateSender("orders-queue");

        Assert.Equal("orders-queue", sender.EntityPath);
    }

    [Fact]
    public void CreateReceiver_ReturnsTrackingServiceBusReceiver()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        var receiver = tracked.CreateReceiver("orders-queue");

        Assert.NotNull(receiver);
        Assert.IsType<TrackingServiceBusReceiver>(receiver);
    }

    [Fact]
    public void CreateReceiver_ReceiverHasCorrectEntityPath()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        var receiver = tracked.CreateReceiver("orders-queue");

        Assert.Equal("orders-queue", receiver.EntityPath);
    }

    [Fact]
    public void CreateReceiver_WithTopicAndSubscription_ReturnsTrackingReceiver()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        var receiver = tracked.CreateReceiver("orders-topic", "my-subscription");

        Assert.NotNull(receiver);
        Assert.IsType<TrackingServiceBusReceiver>(receiver);
    }

    [Fact]
    public void CreateReceiver_WithTopicAndSubscription_HasCorrectEntityPath()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());

        var receiver = tracked.CreateReceiver("orders-topic", "my-subscription");

        Assert.Contains("orders-topic", receiver.EntityPath);
    }

    [Fact]
    public void CreateSender_WithOptions_ReturnsTrackingSender()
    {
        var tracked = new TrackingServiceBusClient(_realClient, MakeOptions());
        var senderOptions = new ServiceBusSenderOptions { Identifier = "my-sender" };

        var sender = tracked.CreateSender("orders-queue", senderOptions);

        Assert.NotNull(sender);
        Assert.Equal("orders-queue", sender.EntityPath);
    }

    [Fact]
    public void Constructor_RegistersTrackerWithRegistry()
    {
        TrackingComponentRegistry.Clear();

        _ = new TrackingServiceBusClient(_realClient, MakeOptions());

        var registered = TrackingComponentRegistry.GetRegisteredComponents();
        Assert.Contains(registered, c => c.ComponentName.Contains("ServiceBusTracker"));
    }
}
