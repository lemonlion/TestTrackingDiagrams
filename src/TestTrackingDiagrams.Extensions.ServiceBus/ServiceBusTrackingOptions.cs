namespace TestTrackingDiagrams.Extensions.ServiceBus;

public record ServiceBusTrackingOptions
{
    public string ServiceName { get; set; } = "ServiceBus";
    public string CallingServiceName { get; set; } = "Caller";
    public ServiceBusTrackingVerbosity Verbosity { get; set; } = ServiceBusTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public ServiceBusTrackingVerbosity? SetupVerbosity { get; set; }
    public ServiceBusTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}
