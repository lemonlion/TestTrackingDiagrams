using Azure.Messaging.ServiceBus;

namespace TestTrackingDiagrams.Extensions.ServiceBus;

public class TrackingServiceBusClient
{
    private readonly ServiceBusClient _inner;
    private readonly ServiceBusTracker _tracker;
    private readonly ServiceBusTrackingOptions _options;

    public TrackingServiceBusClient(ServiceBusClient inner, ServiceBusTrackingOptions options)
    {
        _inner = inner;
        _options = options;
        _tracker = new ServiceBusTracker(options);
    }

    public TrackingServiceBusSender CreateSender(string queueOrTopicName)
    {
        var sender = _inner.CreateSender(queueOrTopicName);
        return new TrackingServiceBusSender(sender, _tracker, _options);
    }

    public TrackingServiceBusSender CreateSender(string queueOrTopicName, ServiceBusSenderOptions senderOptions)
    {
        var sender = _inner.CreateSender(queueOrTopicName, senderOptions);
        return new TrackingServiceBusSender(sender, _tracker, _options);
    }

    public TrackingServiceBusReceiver CreateReceiver(string queueName)
    {
        var receiver = _inner.CreateReceiver(queueName);
        return new TrackingServiceBusReceiver(receiver, _tracker, _options);
    }

    public TrackingServiceBusReceiver CreateReceiver(string queueName, ServiceBusReceiverOptions receiverOptions)
    {
        var receiver = _inner.CreateReceiver(queueName, receiverOptions);
        return new TrackingServiceBusReceiver(receiver, _tracker, _options);
    }

    public TrackingServiceBusReceiver CreateReceiver(string topicName, string subscriptionName)
    {
        var receiver = _inner.CreateReceiver(topicName, subscriptionName);
        return new TrackingServiceBusReceiver(receiver, _tracker, _options);
    }

    public TrackingServiceBusReceiver CreateReceiver(
        string topicName, string subscriptionName, ServiceBusReceiverOptions receiverOptions)
    {
        var receiver = _inner.CreateReceiver(topicName, subscriptionName, receiverOptions);
        return new TrackingServiceBusReceiver(receiver, _tracker, _options);
    }

    public ServiceBusClient Inner => _inner;
    public bool IsClosed => _inner.IsClosed;
    public string FullyQualifiedNamespace => _inner.FullyQualifiedNamespace;
    public string Identifier => _inner.Identifier;
}
