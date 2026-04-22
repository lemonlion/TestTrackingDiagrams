namespace TestTrackingDiagrams.Extensions.EventBridge;

public record EventBridgeOperationInfo(
    EventBridgeOperation Operation,
    string? EventBusName = null,
    string? RuleName = null,
    string? DetailType = null,
    string? Source = null,
    int? EntryCount = null);
