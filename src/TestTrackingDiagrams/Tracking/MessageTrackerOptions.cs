using System.Text.Json;

namespace TestTrackingDiagrams.Tracking;

public record MessageTrackerOptions
{
    /// <summary>
    /// The participant name shown in diagrams for the messaging service when used as
    /// the <c>destinationName</c> default. Also used in <see cref="MessageTracker.ComponentName"/>.
    /// </summary>
    public string ServiceName { get; set; } = "MessageBus";

    /// <summary>
    /// The participant name for the service sending/receiving messages.
    /// </summary>
    public string CallingServiceName { get; set; } = "Caller";

    /// <summary>
    /// Controls how much detail is logged. <see cref="MessageTrackerVerbosity.Summarised"/>
    /// omits message payloads; other levels include them.
    /// </summary>
    public MessageTrackerVerbosity Verbosity { get; set; } = MessageTrackerVerbosity.Detailed;

    /// <summary>
    /// Returns the current test's name and ID. Required for logging — when <c>null</c>,
    /// tracking calls are silently skipped.
    /// </summary>
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }

    /// <summary>
    /// Optional BDD step type fetcher for framework integration.
    /// </summary>
    public Func<string?>? CurrentStepTypeFetcher { get; set; }

    /// <summary>
    /// JSON serialiser options used when serialising message payloads.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }
}
