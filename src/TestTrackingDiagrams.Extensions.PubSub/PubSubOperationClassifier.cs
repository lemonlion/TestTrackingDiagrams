namespace TestTrackingDiagrams.Extensions.PubSub;

public static class PubSubOperationClassifier
{
    public static PubSubOperationInfo Classify(
        string methodName, string? topicName, string? subscriptionName, int? messageCount)
    {
        var operation = methodName switch
        {
            "PublishAsync" when messageCount > 1 => PubSubOperation.PublishBatch,
            "PublishAsync" => PubSubOperation.Publish,
            "PullAsync" => PubSubOperation.Pull,
            "AcknowledgeAsync" => PubSubOperation.Acknowledge,
            "ModifyAckDeadlineAsync" => PubSubOperation.ModifyAckDeadline,
            "Receive" => PubSubOperation.Receive,
            "StartAsync" => PubSubOperation.StartSubscriber,
            "StopAsync" => PubSubOperation.StopSubscriber,
            _ => PubSubOperation.Other
        };

        return new PubSubOperationInfo(operation, topicName, subscriptionName, messageCount);
    }

    public static string GetDiagramLabel(PubSubOperationInfo op, PubSubTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            PubSubTrackingVerbosity.Raw =>
                $"{op.Operation} topic={op.TopicName} sub={op.SubscriptionName}",
            PubSubTrackingVerbosity.Detailed => op.Operation switch
            {
                PubSubOperation.Publish => $"Publish → {ShortName(op.TopicName)}",
                PubSubOperation.PublishBatch => $"Publish (×{op.MessageCount}) → {ShortName(op.TopicName)}",
                PubSubOperation.Pull => $"Pull ← {ShortName(op.SubscriptionName)}",
                PubSubOperation.Receive => $"Receive ← {ShortName(op.SubscriptionName)}",
                PubSubOperation.Acknowledge => "Ack",
                _ => op.Operation.ToString()
            },
            PubSubTrackingVerbosity.Summarised => op.Operation switch
            {
                PubSubOperation.PublishBatch => "Publish",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }

    private static string? ShortName(string? fullName) =>
        fullName?.Split('/').LastOrDefault() ?? fullName;
}
