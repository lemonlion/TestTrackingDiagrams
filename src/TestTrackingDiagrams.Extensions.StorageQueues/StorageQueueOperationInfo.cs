namespace TestTrackingDiagrams.Extensions.StorageQueues;

public record StorageQueueOperationInfo(
    StorageQueueOperation Operation,
    string? QueueName,
    string? MessageId = null);
