namespace TestTrackingDiagrams.Extensions.MassTransit;

public enum MassTransitOperation
{
    Send,
    Publish,
    Consume,
    SendFault,
    PublishFault,
    ConsumeFault,
    Other
}
