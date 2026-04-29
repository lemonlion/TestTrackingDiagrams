namespace TestTrackingDiagrams.Extensions.EventBridge;

/// <summary>
/// The result of classifying a EventBridge operation, containing the operation type and metadata.
/// </summary>
public record EventBridgeOperationInfo(
    EventBridgeOperation Operation,
    string? EventBusName = null,
    string? RuleName = null,
    string? DetailType = null,
    string? Source = null,
    int? EntryCount = null);
