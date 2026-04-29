namespace TestTrackingDiagrams.Extensions.Bigtable;

public record BigtableTrackingOptions
{
    public string ServiceName { get; set; } = "Bigtable";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = "Caller";

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public BigtableTrackingVerbosity Verbosity { get; set; } = BigtableTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public BigtableTrackingVerbosity? SetupVerbosity { get; set; }
    public BigtableTrackingVerbosity? ActionVerbosity { get; set; }
    public HashSet<BigtableOperation> ExcludedOperations { get; set; } = [];
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
