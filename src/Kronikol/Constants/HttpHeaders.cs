namespace Kronikol.Constants;

/// <summary>
/// Defines HTTP header names used by Kronikol for correlating requests across services.
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
/// Defines message header/property names used by Kronikol for propagating
/// test identity through messaging systems (Kafka, ServiceBus, EventHubs, PubSub, MassTransit, etc.).
/// </summary>
public static class TestTrackingMessageHeaders
{
    public const string TestName = "kronikol-test-name";
    public const string TestId = "kronikol-test-id";
}