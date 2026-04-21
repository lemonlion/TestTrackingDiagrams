namespace TestTrackingDiagrams.Extensions.PubSub;

public record PubSubTrackingOptions
{
    public string ServiceName { get; set; } = "PubSub";
    public string CallingServiceName { get; set; } = "Caller";
    public PubSubTrackingVerbosity Verbosity { get; set; } = PubSubTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
}
