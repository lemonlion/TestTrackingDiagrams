namespace TestTrackingDiagrams.Extensions.Spanner;

public record SpannerTrackingOptions
{
    public string ServiceName { get; set; } = "Spanner";
    public string CallingServiceName { get; set; } = "Caller";
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
