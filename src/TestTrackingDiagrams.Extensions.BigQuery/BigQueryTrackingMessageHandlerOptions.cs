namespace TestTrackingDiagrams.Extensions.BigQuery;

public record BigQueryTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "BigQuery";
    public string CallingServiceName { get; set; } = "Caller";
    public BigQueryTrackingVerbosity Verbosity { get; set; } = BigQueryTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }

    /// <summary>
    /// Optional list of headers to exclude from the diagram notes.
    /// Defaults to common noisy Google API headers.
    /// </summary>
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization",
        "User-Agent",
        "x-goog-api-client",
        "x-goog-request-params",
        "google-cloud-resource-prefix",
        "Cache-Control"
    ];
}
