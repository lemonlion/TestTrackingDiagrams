namespace TestTrackingDiagrams.Extensions.StorageQueues;

/// <summary>
/// Classified StorageQueues operation types.
/// </summary>
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
