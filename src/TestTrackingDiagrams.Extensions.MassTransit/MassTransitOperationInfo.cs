namespace TestTrackingDiagrams.Extensions.MassTransit;

public record MassTransitOperationInfo(
    MassTransitOperation Operation,
    string? MessageType,
    Uri? DestinationAddress = null,
    Uri? SourceAddress = null,
    Guid? MessageId = null,
    Guid? ConversationId = null);
