namespace TestTrackingDiagrams.Extensions.Spanner;

public record SpannerTrackingOptions
{
    public string ServiceName { get; set; } = "Spanner";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = "Caller";

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public SpannerTrackingVerbosity Verbosity { get; set; } = SpannerTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public bool LogSqlText { get; set; } = true;
    [Obsolete("Raw verbosity now always includes parameters. This property is no longer used.")]
    public bool LogParameters { get; set; }
    public HashSet<SpannerOperation> ExcludedOperations { get; set; } = [];
    public SpannerTrackingVerbosity? SetupVerbosity { get; set; }
    public SpannerTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
