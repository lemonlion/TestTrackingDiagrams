namespace TestTrackingDiagrams.Extensions.SQS;

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
