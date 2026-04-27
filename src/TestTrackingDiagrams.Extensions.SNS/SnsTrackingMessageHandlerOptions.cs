namespace TestTrackingDiagrams.Extensions.SNS;

public record SnsTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "SNS";
    public string CallingServiceName { get; set; } = "Caller";
    public SnsTrackingVerbosity Verbosity { get; set; } = SnsTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization", "x-amz-date", "x-amz-security-token",
        "x-amz-content-sha256", "User-Agent", "amz-sdk-invocation-id",
        "amz-sdk-request"
    ];
    public SnsTrackingVerbosity? SetupVerbosity { get; set; }
    public SnsTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}
