namespace TestTrackingDiagrams.Extensions.PubSub;

public record PubSubOperationInfo(
    PubSubOperation Operation,
    string? TopicName,
    string? SubscriptionName = null,
    int? MessageCount = null);
