namespace TestTrackingDiagrams.Extensions.MassTransit;

/// <summary>
/// Classified MassTransit operation types.
/// </summary>
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
