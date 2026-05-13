namespace TestTrackingDiagrams.Constants;

/// <summary>
/// Defines HTTP header names used by TestTrackingDiagrams for correlating requests across services.
/// </summary>
public static class TestTrackingHttpHeaders
{
    public const string Ignore = "test-tracking-ignore"; 
    public const string CurrentTestNameHeader = "test-tracking-current-test-name";
    public const string CurrentTestIdHeader = "test-tracking-current-test-id";
    public const string CallerNameHeader = "test-tracking-caller-name";
    public const string TraceIdHeader = "test-tracking-trace-id";
}

/// <summary>
/// Defines message header/property names used by TestTrackingDiagrams for propagating
/// test identity through messaging systems (Kafka, ServiceBus, EventHubs, PubSub, MassTransit, etc.).
/// </summary>
public static class TestTrackingMessageHeaders
{
    public const string TestName = "ttd-test-name";
    public const string TestId = "ttd-test-id";
}