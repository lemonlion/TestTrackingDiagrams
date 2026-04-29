namespace TestTrackingDiagrams.Extensions.Elasticsearch;

/// <summary>
/// Classified Elasticsearch operation types.
/// </summary>
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
