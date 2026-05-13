using TestTrackingDiagrams.Constants;
namespace TestTrackingDiagrams.Extensions.MassTransit;

/// <summary>
/// Configuration options for MassTransit test tracking.
/// </summary>
public record MassTransitTrackingOptions
{
    public string ServiceName { get; set; } = "MassTransit";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

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

    /// <summary>
    /// When <c>true</c>, the send/publish observers inject test identity into MassTransit message headers
    /// and the consume observer extracts them, establishing a <see cref="TestTrackingDiagrams.Tracking.TestIdentityScope"/>
    /// so that downstream tracking operations are attributed to the originating test.
    /// Defaults to <c>true</c>.
    /// </summary>
    public bool PropagateTestIdentity { get; set; } = true;
}