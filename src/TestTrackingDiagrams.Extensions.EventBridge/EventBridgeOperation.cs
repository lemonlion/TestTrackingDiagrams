namespace TestTrackingDiagrams.Extensions.EventBridge;

public enum EventBridgeOperation
{
    // Events
    PutEvents,
    PutPartnerEvents,
    TestEventPattern,

    // Rules
    PutRule,
    DeleteRule,
    DescribeRule,
    EnableRule,
    DisableRule,
    ListRules,

    // Targets
    PutTargets,
    RemoveTargets,
    ListTargetsByRule,

    // Event Buses
    CreateEventBus,
    DeleteEventBus,
    DescribeEventBus,
    ListEventBuses,

    // Archives
    CreateArchive,
    DeleteArchive,
    DescribeArchive,
    ListArchives,

    // Replays
    StartReplay,
    DescribeReplay,
    ListReplays,

    // API Destinations & Connections
    CreateApiDestination,
    CreateConnection,

    // Tags
    TagResource,
    UntagResource,
    ListTagsForResource,

    Other
}
