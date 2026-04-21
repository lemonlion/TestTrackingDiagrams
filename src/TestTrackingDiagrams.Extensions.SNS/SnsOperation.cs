namespace TestTrackingDiagrams.Extensions.SNS;

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
