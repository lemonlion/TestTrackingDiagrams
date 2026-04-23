using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.MediatR;

public record MediatorTrackingOptions
{
    public string ServiceName { get; init; } = "Application";
    public string CallingServiceName { get; init; } = "Caller";
    public string? ActivitySourceName { get; init; }
    public TrackingLogMode LogMode { get; init; } = TrackingLogMode.Immediate;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; init; }
    public TrackingSerializerOptions? SerializerOptions { get; init; }
    public bool TrackDuringSetup { get; init; } = true;
    public bool TrackDuringAction { get; init; } = true;
}
