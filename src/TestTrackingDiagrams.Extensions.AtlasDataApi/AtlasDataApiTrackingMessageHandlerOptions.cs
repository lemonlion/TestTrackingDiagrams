namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

/// <summary>
/// Configuration options for the MongoDB Atlas Data API test tracking message handler.
/// </summary>
public record AtlasDataApiTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "AtlasDataApi";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = "Caller";

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

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