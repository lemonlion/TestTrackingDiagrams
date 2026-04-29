namespace TestTrackingDiagrams.Extensions.SNS;

/// <summary>
/// Classified SNS operation types.
/// </summary>
public enum SnsOperation
{
    Publish,
    PublishBatch,
    Subscribe,
    Unsubscribe,
    CreateTopic,
    DeleteTopic,
    ListTopics,
    ListSubscriptions,
    ListSubscriptionsByTopic,
    GetTopicAttributes,
    SetTopicAttributes,
    ConfirmSubscription,
    Other
}
