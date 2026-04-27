namespace TestTrackingDiagrams.Extensions.SQS;

public record SqsTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "SQS";
    public string CallingServiceName { get; set; } = "Caller";
    public SqsTrackingVerbosity Verbosity { get; set; } = SqsTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization", "x-amz-date", "x-amz-security-token",
        "x-amz-content-sha256", "User-Agent", "amz-sdk-invocation-id",
        "amz-sdk-request"
    ];
    public SqsTrackingVerbosity? SetupVerbosity { get; set; }
    public SqsTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}
