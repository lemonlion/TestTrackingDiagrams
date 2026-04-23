namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Options for configuring the <see cref="TestTrackingMessageHandler"/> that intercepts HTTP traffic for diagram generation.
/// </summary>
public record TestTrackingMessageHandlerOptions
{
    /// <summary>Maps port numbers to human-readable service names for diagram participants.</summary>
    public Dictionary<int, string> PortsToServiceNames { get; set; } = new();

    /// <summary>When set, uses this fixed name for the receiving service instead of inferring from the port.</summary>
    public string? FixedNameForReceivingService { get; set; }

    /// <summary>Display name of the calling service in diagrams. Default: <c>"Caller"</c>.</summary>
    public string CallingServiceName { get; set; } = "Caller";

    /// <summary>HTTP headers to forward from the test context to outgoing requests.</summary>
    public IEnumerable<string> HeadersToForward { get; set; } = [];

    /// <summary>Callback that returns the current test name and ID. Set automatically by framework adapters.</summary>
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; } = null;

    /// <summary>Callback that returns the current test step type (e.g. "Given", "When", "Then"). Set automatically by framework adapters.</summary>
    public Func<string?>? CurrentStepTypeFetcher { get; set; } = null;

    /// <summary>OpenTelemetry activity source names to capture for internal flow diagrams.</summary>
    public string[]? InternalFlowActivitySources { get; set; }

    /// <summary>When <c>false</c>, HTTP requests made during the Setup phase are not tracked. Default: <c>true</c>.</summary>
    public bool TrackDuringSetup { get; set; } = true;

    /// <summary>When <c>false</c>, HTTP requests made during the Action phase are not tracked. Default: <c>true</c>.</summary>
    public bool TrackDuringAction { get; set; } = true;
}