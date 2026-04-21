namespace TestTrackingDiagrams.Extensions.S3;

public record S3TrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "S3";
    public string CallingServiceName { get; set; } = "Caller";
    public S3TrackingVerbosity Verbosity { get; set; } = S3TrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }

    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization",
        "x-amz-date",
        "x-amz-security-token",
        "x-amz-content-sha256",
        "User-Agent",
        "amz-sdk-invocation-id",
        "amz-sdk-request"
    ];
}
