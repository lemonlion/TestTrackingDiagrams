namespace TestTrackingDiagrams.Extensions.Elasticsearch;

public enum ElasticsearchOperation
{
    IndexDocument,
    GetDocument,
    DeleteDocument,
    UpdateDocument,
    Bulk,
    Search,
    MultiSearch,
    Count,
    Scroll,
    CreateIndex,
    DeleteIndex,
    IndexExists,
    PutMapping,
    GetMapping,
    Refresh,
    Reindex,
    DeleteByQuery,
    UpdateByQuery,
    PutIndexTemplate,
    GetIndexTemplate,
    UpdateAliases,
    ClusterHealth,
    CatApis,
    Other
}
