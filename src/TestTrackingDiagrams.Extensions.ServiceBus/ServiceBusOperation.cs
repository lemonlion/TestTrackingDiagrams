namespace TestTrackingDiagrams.Extensions.ServiceBus;

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
