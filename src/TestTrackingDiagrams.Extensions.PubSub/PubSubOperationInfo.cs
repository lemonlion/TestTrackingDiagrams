namespace TestTrackingDiagrams.Extensions.PubSub;

/// <summary>
/// The result of classifying a PubSub operation, containing the operation type and metadata.
/// </summary>
public record PubSubOperationInfo(
    PubSubOperation Operation,
    string? TopicName,
    string? SubscriptionName = null,
    int? MessageCount = null);
