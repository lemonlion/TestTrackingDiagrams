namespace TestTrackingDiagrams.Extensions.MongoDB;

public record MongoDbTrackingOptions
{
    public string ServiceName { get; set; } = "MongoDB";
    public string CallingServiceName { get; set; } = "Caller";
    public MongoDbTrackingVerbosity Verbosity { get; set; } = MongoDbTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }

    /// <summary>
    /// Commands to ignore (e.g., monitoring noise like "isMaster", "hello", "ping").
    /// </summary>
    public HashSet<string> IgnoredCommands { get; set; } =
    [
        "isMaster", "hello", "saslStart", "saslContinue",
        "ping", "buildInfo", "getLastError",
        "killCursors"
    ];

    /// <summary>
    /// Whether to track getMore (cursor continuation) operations.
    /// Disabled by default as they add noise.
    /// </summary>
    public bool TrackGetMore { get; set; } = false;
    public MongoDbTrackingVerbosity? SetupVerbosity { get; set; }
    public MongoDbTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
