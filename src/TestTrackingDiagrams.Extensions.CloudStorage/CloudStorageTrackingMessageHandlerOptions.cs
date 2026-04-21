namespace TestTrackingDiagrams.Extensions.CloudStorage;

public record CloudStorageTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "CloudStorage";
    public string CallingServiceName { get; set; } = "Caller";
    public CloudStorageTrackingVerbosity Verbosity { get; set; } = CloudStorageTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization", "x-goog-api-client", "User-Agent"
    ];
}
