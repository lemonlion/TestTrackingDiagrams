namespace TestTrackingDiagrams.Tracking;

public record TestTrackingMessageHandlerOptions
{
    public Dictionary<int, string> PortsToServiceNames { get; set; } = new();
    public string? FixedNameForReceivingService { get; set; }
    public string CallingServiceName { get; set; } = "Caller";
    public IEnumerable<string> HeadersToForward { get; set; } = [];
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; } = null;
}