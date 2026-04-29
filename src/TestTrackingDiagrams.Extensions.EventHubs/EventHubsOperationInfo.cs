namespace TestTrackingDiagrams.Extensions.EventHubs;

/// <summary>
/// The result of classifying a EventHubs operation, containing the operation type and metadata.
/// </summary>
public record EventHubsOperationInfo(
    EventHubsOperation Operation,
    string? EventHubName,
    string? PartitionId = null,
    int? EventCount = null);
