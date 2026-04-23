namespace TestTrackingDiagrams.Extensions.Grpc;

public record GrpcTrackingOptions
{
    public string ServiceName { get; set; } = "GrpcService";
    public string CallingServiceName { get; set; } = "Caller";
    public GrpcTrackingVerbosity Verbosity { get; set; } = GrpcTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public bool UseProtoServiceNameInDiagram { get; set; } = false;
    public GrpcTrackingVerbosity? SetupVerbosity { get; set; }
    public GrpcTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
}
