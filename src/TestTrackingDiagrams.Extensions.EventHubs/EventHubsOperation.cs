namespace TestTrackingDiagrams.Extensions.EventHubs;

/// <summary>
/// Classified EventHubs operation types.
/// </summary>
public enum EventHubsOperation
{
    Send,
    SendBatch,
    CreateBatch,
    ReadEvents,
    ReadEventsFromPartition,
    GetPartitionIds,
    GetEventHubProperties,
    GetPartitionProperties,
    StartProcessing,
    StopProcessing,
    ProcessEvent,
    Other
}
