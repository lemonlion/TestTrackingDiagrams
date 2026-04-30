using TestTrackingDiagrams.Constants;
namespace TestTrackingDiagrams.Extensions.EventHubs;

/// <summary>
/// Configuration options for Azure Event Hubs test tracking.
/// </summary>
public record EventHubsTrackingOptions
{
    public string ServiceName { get; set; } = "EventHubs";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public EventHubsTrackingVerbosity Verbosity { get; set; } = EventHubsTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public EventHubsTrackingVerbosity? SetupVerbosity { get; set; }
    public EventHubsTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}