namespace TestTrackingDiagrams.Extensions.Kafka;

public record KafkaTrackingOptions
{
    public string ServiceName { get; set; } = "Kafka";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = "Caller";

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
}
