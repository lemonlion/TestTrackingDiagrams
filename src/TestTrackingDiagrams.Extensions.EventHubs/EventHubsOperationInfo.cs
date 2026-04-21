namespace TestTrackingDiagrams.Extensions.EventHubs;

public record EventHubsOperationInfo(
    EventHubsOperation Operation,
    string? EventHubName,
    string? PartitionId = null,
    int? EventCount = null);
