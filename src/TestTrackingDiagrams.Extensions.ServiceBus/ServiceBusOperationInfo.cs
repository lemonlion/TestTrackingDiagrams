namespace TestTrackingDiagrams.Extensions.ServiceBus;

/// <summary>
/// The result of classifying a ServiceBus operation, containing the operation type and metadata.
/// </summary>
public record ServiceBusOperationInfo(
    ServiceBusOperation Operation,
    string? QueueOrTopicName,
    string? SubscriptionName = null,
    int? MessageCount = null);
