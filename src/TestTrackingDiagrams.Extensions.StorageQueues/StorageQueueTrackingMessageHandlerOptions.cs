namespace TestTrackingDiagrams.Extensions.StorageQueues;

public record StorageQueueTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "StorageQueue";
    public string CallingServiceName { get; set; } = "Caller";
    public StorageQueueTrackingVerbosity Verbosity { get; set; } = StorageQueueTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization", "x-ms-date", "x-ms-version",
        "x-ms-client-request-id", "x-ms-return-client-request-id",
        "User-Agent", "Cache-Control"
    ];
}
