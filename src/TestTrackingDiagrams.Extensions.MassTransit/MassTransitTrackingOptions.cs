namespace TestTrackingDiagrams.Extensions.MassTransit;

public record MassTransitTrackingOptions
{
    public string ServiceName { get; set; } = "MassTransit";
    public string CallingServiceName { get; set; } = "Caller";
    public MassTransitTrackingVerbosity Verbosity { get; set; } = MassTransitTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public bool TrackSend { get; set; } = true;
    public bool TrackPublish { get; set; } = true;
    public bool TrackConsume { get; set; } = true;
    public bool LogMessageBody { get; set; } = true;
    public bool LogFaults { get; set; } = true;
    public MassTransitTrackingVerbosity? SetupVerbosity { get; set; }
    public MassTransitTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}
