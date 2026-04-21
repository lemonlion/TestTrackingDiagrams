namespace TestTrackingDiagrams.Extensions.StorageQueues;

public enum StorageQueueOperation
{
    SendMessage,
    ReceiveMessages,
    PeekMessages,
    DeleteMessage,
    UpdateMessage,
    ClearMessages,
    CreateQueue,
    DeleteQueue,
    GetProperties,
    SetMetadata,
    ListQueues,
    Other
}
