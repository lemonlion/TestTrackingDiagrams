using TestTrackingDiagrams.Constants;
namespace TestTrackingDiagrams.Extensions.Elasticsearch;

/// <summary>
/// Configuration options for Elasticsearch test tracking.
/// </summary>
public class ElasticsearchTrackingOptions
{
    public string ServiceName { get; set; } = "Elasticsearch";

    /// <summary>The participant name for the calling service in diagrams.</summary>
    public string CallerName { get; set; } = TrackingDefaults.CallerName;

    /// <summary>Use <see cref="CallerName"/> instead.</summary>
    [Obsolete("Use CallerName instead. CallingServiceName will be removed in a future version.")]
    public string CallingServiceName { get => CallerName; set => CallerName = value; }

    public ElasticsearchTrackingVerbosity Verbosity { get; set; } = ElasticsearchTrackingVerbosity.Detailed;
    public Func<(string Name, string Id)>? CurrentTestInfoFetcher { get; set; }
    public Func<string?>? CurrentStepTypeFetcher { get; set; }
    public HashSet<ElasticsearchOperation> ExcludedOperations { get; set; } =
    [
        ElasticsearchOperation.ClusterHealth,
        ElasticsearchOperation.CatApis
    ];
    public ElasticsearchTrackingVerbosity? SetupVerbosity { get; set; }
    public ElasticsearchTrackingVerbosity? ActionVerbosity { get; set; }
    public bool TrackDuringSetup { get; set; } = true;
    public bool TrackDuringAction { get; set; } = true;
    public Microsoft.AspNetCore.Http.IHttpContextAccessor? HttpContextAccessor { get; set; }
}