namespace TestTrackingDiagrams.Extensions.Elasticsearch;

public class ElasticsearchTrackingOptions
{
    public string ServiceName { get; set; } = "Elasticsearch";
    public string CallingServiceName { get; set; } = "Caller";
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
