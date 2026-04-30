using TestTrackingDiagrams.Constants;
namespace TestTrackingDiagrams.Extensions.EventBridge;

/// <summary>
/// Configuration options for the Amazon EventBridge test tracking message handler.
/// </summary>
public record EventBridgeTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "EventBridge";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

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
    public EventBridgeTrackingVerbosity? SetupVerbosity { get; set; }
    public EventBridgeTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}