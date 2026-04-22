namespace TestTrackingDiagrams.Extensions.EventBridge;

public record EventBridgeTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "EventBridge";
    public string CallingServiceName { get; set; } = "Caller";
    public EventBridgeTrackingVerbosity Verbosity { get; set; } = EventBridgeTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<EventBridgeOperation> ExcludedOperations { get; set; } =
    [
        EventBridgeOperation.TagResource,
        EventBridgeOperation.UntagResource,
        EventBridgeOperation.ListTagsForResource,
        EventBridgeOperation.ListEventBuses
    ];
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization", "x-amz-date", "x-amz-security-token",
        "x-amz-content-sha256", "User-Agent", "amz-sdk-invocation-id",
        "amz-sdk-request"
    ];
}
