namespace TestTrackingDiagrams.Extensions.PubSub;

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
