namespace TestTrackingDiagrams.Extensions.StorageQueues;

/// <summary>
/// The result of classifying a StorageQueues operation, containing the operation type and metadata.
/// </summary>
public record StorageQueueOperationInfo(
    StorageQueueOperation Operation,
    string? QueueName,
    string? MessageId = null);
