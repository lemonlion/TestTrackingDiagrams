namespace TestTrackingDiagrams.Extensions.DynamoDB;

public record DynamoDbTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "DynamoDB";
    public string CallingServiceName { get; set; } = "Caller";
    public DynamoDbTrackingVerbosity Verbosity { get; set; } = DynamoDbTrackingVerbosity.Detailed;
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
    public DynamoDbTrackingVerbosity? SetupVerbosity { get; set; }
    public DynamoDbTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
