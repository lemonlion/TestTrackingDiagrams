namespace TestTrackingDiagrams.Extensions.EventHubs;

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
