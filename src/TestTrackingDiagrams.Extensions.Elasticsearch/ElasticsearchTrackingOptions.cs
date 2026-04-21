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
}
