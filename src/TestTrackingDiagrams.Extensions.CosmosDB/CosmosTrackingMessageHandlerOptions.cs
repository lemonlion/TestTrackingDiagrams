using TestTrackingDiagrams.Constants;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Extensions.CosmosDB;

/// <summary>
/// Configuration options for the Azure Cosmos DB test tracking message handler.
/// </summary>
public record CosmosTrackingMessageHandlerOptions
{
    public string ServiceName { get; set; } = "CosmosDB";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public CosmosTrackingVerbosity Verbosity { get; set; } = CosmosTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }

    /// <summary>
    /// Optional list of Cosmos headers to exclude from the diagram notes.
    /// Defaults to common noisy headers.
    /// </summary>
    public HashSet<string> ExcludedHeaders { get; set; } =
    [
        "Authorization",
        "x-ms-date",
        "x-ms-version",
        "x-ms-session-token",
        "User-Agent",
        "Cache-Control",
        "x-ms-cosmos-sdk-supportedcapabilities",
        "x-ms-cosmos-internal-operation-type"
    ];
    public CosmosTrackingVerbosity? SetupVerbosity { get; set; }
    public CosmosTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}