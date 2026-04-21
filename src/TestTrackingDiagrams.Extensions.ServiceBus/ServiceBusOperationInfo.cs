namespace TestTrackingDiagrams.Extensions.ServiceBus;

public record ServiceBusOperationInfo(
    ServiceBusOperation Operation,
    string? QueueOrTopicName,
    string? SubscriptionName = null,
    int? MessageCount = null);
