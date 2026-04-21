namespace TestTrackingDiagrams.Extensions.ServiceBus;

public static class ServiceBusOperationClassifier
{
    public static ServiceBusOperationInfo Classify(string methodName, string? entityPath, object?[]? args)
    {
        var queueOrTopic = string.IsNullOrEmpty(entityPath) ? null : entityPath;

        int? messageCount = null;
        if (args is { Length: > 0 } && args[0] is ICollection<object> collection)
            messageCount = collection.Count;
        else if (args is { Length: > 0 } && args[0] is System.Collections.ICollection legacyCollection)
            messageCount = legacyCollection.Count;

        var operation = methodName switch
        {
            "SendMessageAsync" => ServiceBusOperation.Send,
            "SendMessagesAsync" => ServiceBusOperation.SendBatch,
            "ScheduleMessageAsync" => ServiceBusOperation.Schedule,
            "ScheduleMessagesAsync" => ServiceBusOperation.Schedule,
            "CancelScheduledMessageAsync" => ServiceBusOperation.CancelSchedule,
            "CancelScheduledMessagesAsync" => ServiceBusOperation.CancelSchedule,
            "ReceiveMessageAsync" => ServiceBusOperation.Receive,
            "ReceiveMessagesAsync" => ServiceBusOperation.ReceiveBatch,
            "PeekMessageAsync" => ServiceBusOperation.Peek,
            "PeekMessagesAsync" => ServiceBusOperation.Peek,
            "CompleteMessageAsync" => ServiceBusOperation.Complete,
            "AbandonMessageAsync" => ServiceBusOperation.Abandon,
            "DeadLetterMessageAsync" => ServiceBusOperation.DeadLetter,
            "DeferMessageAsync" => ServiceBusOperation.Defer,
            "RenewMessageLockAsync" => ServiceBusOperation.RenewMessageLock,
            "RenewSessionLockAsync" => ServiceBusOperation.RenewSessionLock,
            "GetSessionStateAsync" => ServiceBusOperation.GetSessionState,
            "SetSessionStateAsync" => ServiceBusOperation.SetSessionState,
            "StartProcessingAsync" => ServiceBusOperation.StartProcessing,
            "StopProcessingAsync" => ServiceBusOperation.StopProcessing,
            _ => ServiceBusOperation.Other
        };

        return new ServiceBusOperationInfo(operation, queueOrTopic, MessageCount: messageCount);
    }

    public static string GetDiagramLabel(ServiceBusOperationInfo op, ServiceBusTrackingVerbosity verbosity)
    {
        if (verbosity == ServiceBusTrackingVerbosity.Raw)
            return op.Operation.ToString();

        if (verbosity == ServiceBusTrackingVerbosity.Summarised)
        {
            return op.Operation switch
            {
                ServiceBusOperation.Send or ServiceBusOperation.SendBatch => "Send",
                ServiceBusOperation.Schedule => "Schedule",
                ServiceBusOperation.CancelSchedule => "CancelSchedule",
                ServiceBusOperation.Receive or ServiceBusOperation.ReceiveBatch => "Receive",
                ServiceBusOperation.Peek => "Peek",
                ServiceBusOperation.Complete => "Complete",
                ServiceBusOperation.Abandon => "Abandon",
                ServiceBusOperation.DeadLetter => "DeadLetter",
                ServiceBusOperation.Defer => "Defer",
                ServiceBusOperation.RenewMessageLock => "RenewLock",
                ServiceBusOperation.RenewSessionLock => "RenewSessionLock",
                ServiceBusOperation.GetSessionState => "GetSessionState",
                ServiceBusOperation.SetSessionState => "SetSessionState",
                ServiceBusOperation.StartProcessing => "StartProcessing",
                ServiceBusOperation.StopProcessing => "StopProcessing",
                _ => op.Operation.ToString()
            };
        }

        // Detailed
        var queue = op.QueueOrTopicName;
        return op.Operation switch
        {
            ServiceBusOperation.Send => queue is not null ? $"Send → {queue}" : "Send",
            ServiceBusOperation.SendBatch => queue is not null
                ? (op.MessageCount.HasValue ? $"Send (×{op.MessageCount}) → {queue}" : $"Send (batch) → {queue}")
                : (op.MessageCount.HasValue ? $"Send (×{op.MessageCount})" : "Send (batch)"),
            ServiceBusOperation.Schedule => queue is not null ? $"Schedule → {queue}" : "Schedule",
            ServiceBusOperation.CancelSchedule => "CancelSchedule",
            ServiceBusOperation.Receive => queue is not null ? $"Receive ← {queue}" : "Receive",
            ServiceBusOperation.ReceiveBatch => queue is not null
                ? (op.MessageCount.HasValue ? $"Receive (×{op.MessageCount}) ← {queue}" : $"Receive (batch) ← {queue}")
                : (op.MessageCount.HasValue ? $"Receive (×{op.MessageCount})" : "Receive (batch)"),
            ServiceBusOperation.Peek => queue is not null ? $"Peek ← {queue}" : "Peek",
            ServiceBusOperation.Complete => "Complete",
            ServiceBusOperation.Abandon => "Abandon",
            ServiceBusOperation.DeadLetter => "DeadLetter",
            ServiceBusOperation.Defer => "Defer",
            ServiceBusOperation.RenewMessageLock => "RenewLock",
            ServiceBusOperation.RenewSessionLock => "RenewSessionLock",
            ServiceBusOperation.GetSessionState => "GetSessionState",
            ServiceBusOperation.SetSessionState => "SetSessionState",
            ServiceBusOperation.StartProcessing => "StartProcessing",
            ServiceBusOperation.StopProcessing => "StopProcessing",
            _ => op.Operation.ToString()
        };
    }
}
