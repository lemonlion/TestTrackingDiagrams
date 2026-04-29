namespace TestTrackingDiagrams.Extensions.ServiceBus;

/// <summary>
/// Classified ServiceBus operation types.
/// </summary>
public enum ServiceBusOperation
{
    Send,
    SendBatch,
    Schedule,
    CancelSchedule,
    Receive,
    ReceiveBatch,
    Peek,
    Complete,
    Abandon,
    DeadLetter,
    Defer,
    RenewMessageLock,
    RenewSessionLock,
    GetSessionState,
    SetSessionState,
    StartProcessing,
    StopProcessing,
    Other
}
