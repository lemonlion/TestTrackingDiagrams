namespace TestTrackingDiagrams.Extensions.EventHubs;

public record EventHubsTrackingOptions
{
    public string ServiceName { get; set; } = "EventHubs";
    public string CallingServiceName { get; set; } = "Caller";
    public EventHubsTrackingVerbosity Verbosity { get; set; } = EventHubsTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public EventHubsTrackingVerbosity? SetupVerbosity { get; set; }
    public EventHubsTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}
