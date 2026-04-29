namespace TestTrackingDiagrams.Extensions.MassTransit;

/// <summary>
/// The result of classifying a MassTransit operation, containing the operation type and metadata.
/// </summary>
public record MassTransitOperationInfo(
    MassTransitOperation Operation,
    string? MessageType,
    Uri? DestinationAddress = null,
    Uri? SourceAddress = null,
    Guid? MessageId = null,
    Guid? ConversationId = null);
