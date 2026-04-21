namespace TestTrackingDiagrams.Extensions.ServiceBus;

public record ServiceBusTrackingOptions
{
    public string ServiceName { get; set; } = "ServiceBus";
    public string CallingServiceName { get; set; } = "Caller";
    public ServiceBusTrackingVerbosity Verbosity { get; set; } = ServiceBusTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
}
