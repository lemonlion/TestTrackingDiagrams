using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.MediatR;

/// <summary>
/// Configuration options for MediatR test tracking.
/// </summary>
public record MediatorTrackingOptions
{
    public string ServiceName { get; init; } = "Application";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; init; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; init => CallerName = value; }
    public string? ActivitySourceName { get; init; }
    public TrackingLogMode LogMode { get; init; } = TrackingLogMode.Immediate;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; init; }
    public TrackingSerializerOptions? SerializerOptions { get; init; }
    public bool TrackDuringSetup { get; init; } = true;
    public bool TrackDuringAction { get; init; } = true;
}