namespace TestTrackingDiagrams.Extensions.PubSub;

/// <summary>
/// Classified PubSub operation types.
/// </summary>
public enum PubSubOperation
{
    Publish,
    PublishBatch,
    Pull,
    Acknowledge,
    ModifyAckDeadline,
    Receive,
    StartSubscriber,
    StopSubscriber,
    Other
}
