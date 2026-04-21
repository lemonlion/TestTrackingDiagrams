namespace TestTrackingDiagrams.Extensions.EventHubs;

public static class EventHubsOperationClassifier
{
    public static EventHubsOperationInfo Classify(
        string methodName, string? eventHubName, string? partitionId = null, int? eventCount = null)
    {
        var operation = methodName switch
        {
            "SendAsync" when eventCount > 1 => EventHubsOperation.SendBatch,
            "SendAsync" => EventHubsOperation.Send,
            "CreateBatchAsync" => EventHubsOperation.CreateBatch,
            "ReadEventsAsync" => EventHubsOperation.ReadEvents,
            "ReadEventsFromPartitionAsync" => EventHubsOperation.ReadEventsFromPartition,
            "GetPartitionIdsAsync" => EventHubsOperation.GetPartitionIds,
            "GetEventHubPropertiesAsync" => EventHubsOperation.GetEventHubProperties,
            "GetPartitionPropertiesAsync" => EventHubsOperation.GetPartitionProperties,
            "StartProcessingAsync" => EventHubsOperation.StartProcessing,
            "StopProcessingAsync" => EventHubsOperation.StopProcessing,
            "ProcessEvent" => EventHubsOperation.ProcessEvent,
            _ => EventHubsOperation.Other
        };

        return new EventHubsOperationInfo(operation, eventHubName, partitionId, eventCount);
    }

    public static string GetDiagramLabel(EventHubsOperationInfo op, EventHubsTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            EventHubsTrackingVerbosity.Raw =>
                $"{op.Operation} hub={op.EventHubName} partition={op.PartitionId} count={op.EventCount}",
            EventHubsTrackingVerbosity.Detailed => op.Operation switch
            {
                EventHubsOperation.Send => $"Send → {op.EventHubName}",
                EventHubsOperation.SendBatch => $"Send (×{op.EventCount}) → {op.EventHubName}",
                EventHubsOperation.ReadEvents => $"Read ← {op.EventHubName}",
                EventHubsOperation.ReadEventsFromPartition =>
                    $"Read ← {op.EventHubName}[{op.PartitionId}]",
                EventHubsOperation.ProcessEvent => $"Process ← {op.EventHubName}",
                _ => op.Operation.ToString()
            },
            EventHubsTrackingVerbosity.Summarised => op.Operation switch
            {
                EventHubsOperation.SendBatch => "Send",
                EventHubsOperation.ReadEventsFromPartition => "Read",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }
}
