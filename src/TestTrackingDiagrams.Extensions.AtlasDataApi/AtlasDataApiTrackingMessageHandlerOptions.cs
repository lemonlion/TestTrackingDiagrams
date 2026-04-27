namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

public record AtlasDataApiTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "AtlasDataApi";
    public string CallingServiceName { get; set; } = "Caller";
    public AtlasDataApiTrackingVerbosity Verbosity { get; set; } = AtlasDataApiTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization",
        "User-Agent",
        "api-key",
        "apiKey"
    ];
    public HashSet<AtlasDataApiOperation> ExcludedOperations { get; set; } = [];
    public AtlasDataApiTrackingVerbosity? SetupVerbosity { get; set; }
    public AtlasDataApiTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
