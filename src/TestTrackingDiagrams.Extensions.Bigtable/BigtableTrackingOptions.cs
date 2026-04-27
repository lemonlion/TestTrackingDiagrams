namespace TestTrackingDiagrams.Extensions.Bigtable;

public record BigtableTrackingOptions
{
    public string ServiceName { get; set; } = "Bigtable";
    public string CallingServiceName { get; set; } = "Caller";
    public BigtableTrackingVerbosity Verbosity { get; set; } = BigtableTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public BigtableTrackingVerbosity? SetupVerbosity { get; set; }
    public BigtableTrackingVerbosity? ActionVerbosity { get; set; }
    public HashSet<BigtableOperation> ExcludedOperations { get; set; } = [];
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
