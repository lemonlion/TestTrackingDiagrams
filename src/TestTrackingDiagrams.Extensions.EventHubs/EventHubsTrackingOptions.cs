namespace TestTrackingDiagrams.Extensions.EventHubs;

public record EventHubsTrackingOptions
{
    public string ServiceName { get; set; } = "EventHubs";
    public string CallingServiceName { get; set; } = "Caller";
    public EventHubsTrackingVerbosity Verbosity { get; set; } = EventHubsTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
}
