namespace TestTrackingDiagrams.Extensions.SQS;

/// <summary>
/// Classified SQS operation types.
/// </summary>
public enum SqsOperation
{
    SendMessage,
    SendMessageBatch,
    ReceiveMessage,
    DeleteMessage,
    DeleteMessageBatch,
    ChangeMessageVisibility,
    ChangeMessageVisibilityBatch,
    CreateQueue,
    DeleteQueue,
    GetQueueUrl,
    GetQueueAttributes,
    SetQueueAttributes,
    PurgeQueue,
    ListQueues,
    Other
}
