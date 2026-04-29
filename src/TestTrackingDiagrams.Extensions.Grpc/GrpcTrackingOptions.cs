using Microsoft.AspNetCore.Http;

namespace TestTrackingDiagrams.Extensions.Grpc;

public record GrpcTrackingOptions
{
    public string ServiceName { get; set; } = "GrpcService";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = "Caller";

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public GrpcTrackingVerbosity Verbosity { get; set; } = GrpcTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public bool UseProtoServiceNameInDiagram { get; set; } = false;
    public GrpcTrackingVerbosity? SetupVerbosity { get; set; }
    public GrpcTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public IHttpContextAccessor? HttpContextAccessor { get; set; }
}
