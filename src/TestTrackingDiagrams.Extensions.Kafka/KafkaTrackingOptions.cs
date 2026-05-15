using TestTrackingDiagrams.Constants;
namespace TestTrackingDiagrams.Extensions.Kafka;

/// <summary>
/// Configuration options for Apache Kafka test tracking.
/// </summary>
public record KafkaTrackingOptions
{
    public string ServiceName { get; set; } = "Kafka";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public KafkaTrackingVerbosity Verbosity { get; set; } = KafkaTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public bool TrackProduce { get; set; } = true;
    public bool TrackConsume { get; set; } = true;
    public bool LogMessageValue { get; set; } = true;
    public bool LogMessageKey { get; set; } = true;
    public bool TrackSubscribe { get; set; } = false;
    public bool TrackUnsubscribe { get; set; } = false;
    public bool TrackCommit { get; set; } = false;
    public bool TrackFlush { get; set; } = false;
    public bool TrackTransactions { get; set; } = false;
    public KafkaTrackingVerbosity? SetupVerbosity { get; set; }
    public KafkaTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the producer injects test identity headers into Kafka message headers
    /// and the consumer extracts them, establishing a <see cref="TestTrackingDiagrams.Tracking.TestIdentityScope"/>
    /// so that downstream tracking operations are attributed to the originating test.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool PropagateTestIdentity { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the consumer stores a correlation entry in <see cref="TestTrackingDiagrams.Tracking.TestCorrelationStore"/>
    /// after extracting test identity from message headers. This enables parallel-safe attribution
    /// for decoupled processing patterns where the processing thread loses access to the message headers.
    /// Default: <c>true</c>.
    /// </summary>
    public bool AutoCorrelateOnConsume { get; set; } = true;

    /// <summary>
    /// Extracts a correlation key from a consumed message key.
    /// When <c>null</c>, the default format is used: <c>kafka:{ServiceName}:{messageKey}</c>.
    /// Only invoked when <see cref="AutoCorrelateOnConsume"/> is <c>true</c>.
    /// </summary>
    public Func<string, string, string>? ConsumeKeyExtractor { get; set; }
}