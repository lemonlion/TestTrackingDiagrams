using TestTrackingDiagrams.Extensions.Elasticsearch;

namespace TestTrackingDiagrams.Tests.Elasticsearch;

public class ElasticsearchOperationClassifierTests
{
    private static Uri MakeUri(string path) => new($"http://localhost:9200{path}");

    // ─── Document operations ───────────────────────────────────

    [Fact]
    public void POST_index_doc_classifies_as_IndexDocument()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_doc"));
        Assert.Equal(ElasticsearchOperation.IndexDocument, result.Operation);
        Assert.Equal("orders", result.IndexName);
        Assert.Null(result.DocumentId);
    }

    [Fact]
    public void PUT_index_doc_id_classifies_as_IndexDocument_with_id()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Put, MakeUri("/orders/_doc/123"));
        Assert.Equal(ElasticsearchOperation.IndexDocument, result.Operation);
        Assert.Equal("orders", result.IndexName);
        Assert.Equal("123", result.DocumentId);
    }

    [Fact]
    public void GET_index_doc_id_classifies_as_GetDocument()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/orders/_doc/123"));
        Assert.Equal(ElasticsearchOperation.GetDocument, result.Operation);
        Assert.Equal("orders", result.IndexName);
        Assert.Equal("123", result.DocumentId);
    }

    [Fact]
    public void DELETE_index_doc_id_classifies_as_DeleteDocument()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Delete, MakeUri("/orders/_doc/abc"));
        Assert.Equal(ElasticsearchOperation.DeleteDocument, result.Operation);
        Assert.Equal("orders", result.IndexName);
        Assert.Equal("abc", result.DocumentId);
    }

    [Fact]
    public void POST_index_update_id_classifies_as_UpdateDocument()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_update/123"));
        Assert.Equal(ElasticsearchOperation.UpdateDocument, result.Operation);
        Assert.Equal("orders", result.IndexName);
        Assert.Equal("123", result.DocumentId);
    }

    // ─── Search operations ──────────────────────────────────────

    [Fact]
    public void POST_index_search_classifies_as_Search()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_search"));
        Assert.Equal(ElasticsearchOperation.Search, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void POST_index_count_classifies_as_Count()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_count"));
        Assert.Equal(ElasticsearchOperation.Count, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void POST_msearch_classifies_as_MultiSearch()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/_msearch"));
        Assert.Equal(ElasticsearchOperation.MultiSearch, result.Operation);
        Assert.Null(result.IndexName);
    }

    // ─── Bulk ───────────────────────────────────────────────────

    [Fact]
    public void POST_bulk_classifies_as_Bulk_without_index()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/_bulk"));
        Assert.Equal(ElasticsearchOperation.Bulk, result.Operation);
        Assert.Null(result.IndexName);
    }

    [Fact]
    public void POST_index_bulk_classifies_as_Bulk_with_index()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_bulk"));
        Assert.Equal(ElasticsearchOperation.Bulk, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    // ─── By-query operations ────────────────────────────────────

    [Fact]
    public void POST_delete_by_query_classifies_correctly()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_delete_by_query"));
        Assert.Equal(ElasticsearchOperation.DeleteByQuery, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void POST_update_by_query_classifies_correctly()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_update_by_query"));
        Assert.Equal(ElasticsearchOperation.UpdateByQuery, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    // ─── Index management ───────────────────────────────────────

    [Fact]
    public void PUT_index_classifies_as_CreateIndex()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Put, MakeUri("/orders"));
        Assert.Equal(ElasticsearchOperation.CreateIndex, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void DELETE_index_classifies_as_DeleteIndex()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Delete, MakeUri("/orders"));
        Assert.Equal(ElasticsearchOperation.DeleteIndex, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void HEAD_index_classifies_as_IndexExists()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Head, MakeUri("/orders"));
        Assert.Equal(ElasticsearchOperation.IndexExists, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void PUT_index_mapping_classifies_as_PutMapping()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Put, MakeUri("/orders/_mapping"));
        Assert.Equal(ElasticsearchOperation.PutMapping, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void GET_index_mapping_classifies_as_GetMapping()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/orders/_mapping"));
        Assert.Equal(ElasticsearchOperation.GetMapping, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void POST_index_refresh_classifies_as_Refresh()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/orders/_refresh"));
        Assert.Equal(ElasticsearchOperation.Refresh, result.Operation);
        Assert.Equal("orders", result.IndexName);
    }

    [Fact]
    public void POST_reindex_classifies_correctly()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/_reindex"));
        Assert.Equal(ElasticsearchOperation.Reindex, result.Operation);
    }

    // ─── Template/Alias ─────────────────────────────────────────

    [Fact]
    public void PUT_index_template_classifies_as_PutIndexTemplate()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Put, MakeUri("/_index_template/my-template"));
        Assert.Equal(ElasticsearchOperation.PutIndexTemplate, result.Operation);
    }

    [Fact]
    public void GET_index_template_classifies_as_GetIndexTemplate()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/_index_template/my-template"));
        Assert.Equal(ElasticsearchOperation.GetIndexTemplate, result.Operation);
    }

    [Fact]
    public void POST_aliases_classifies_as_UpdateAliases()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/_aliases"));
        Assert.Equal(ElasticsearchOperation.UpdateAliases, result.Operation);
    }

    // ─── Cluster ────────────────────────────────────────────────

    [Fact]
    public void GET_cluster_health_classifies_correctly()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/_cluster/health"));
        Assert.Equal(ElasticsearchOperation.ClusterHealth, result.Operation);
    }

    [Fact]
    public void GET_cat_indices_classifies_as_CatApis()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/_cat/indices"));
        Assert.Equal(ElasticsearchOperation.CatApis, result.Operation);
    }

    // ─── Scroll ─────────────────────────────────────────────────

    [Fact]
    public void POST_search_scroll_classifies_as_Scroll()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/_search/scroll"));
        Assert.Equal(ElasticsearchOperation.Scroll, result.Operation);
    }

    // ─── Unknown ────────────────────────────────────────────────

    [Fact]
    public void Unknown_path_classifies_as_Other()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/_unknown/something"));
        Assert.Equal(ElasticsearchOperation.Other, result.Operation);
    }

    // ─── Diagram labels ─────────────────────────────────────────

    [Fact]
    public void Detailed_label_for_Search()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.Search, "orders");
        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, ElasticsearchTrackingVerbosity.Detailed);
        Assert.Equal("Search → orders", label);
    }

    [Fact]
    public void Detailed_label_for_IndexDocument()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.IndexDocument, "orders");
        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, ElasticsearchTrackingVerbosity.Detailed);
        Assert.Equal("Index → orders", label);
    }

    [Fact]
    public void Detailed_label_for_GetDocument()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.GetDocument, "orders", "123");
        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, ElasticsearchTrackingVerbosity.Detailed);
        Assert.Equal("Get ← orders", label);
    }

    [Fact]
    public void Summarised_label_for_Search()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.Search, "orders");
        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, ElasticsearchTrackingVerbosity.Summarised);
        Assert.Equal("Search", label);
    }

    [Fact]
    public void Raw_label_includes_index_and_doc_id()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.GetDocument, "orders", "123");
        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, ElasticsearchTrackingVerbosity.Raw);
        Assert.Equal("GetDocument /orders/123", label);
    }

    [Fact]
    public void Raw_label_without_doc_id()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.Search, "orders");
        var label = ElasticsearchOperationClassifier.GetDiagramLabel(op, ElasticsearchTrackingVerbosity.Raw);
        Assert.Equal("Search /orders", label);
    }

    // ─── URI building ───────────────────────────────────────────

    [Fact]
    public void BuildUri_Detailed_uses_elasticsearch_scheme()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.Search, "orders");
        var uri = ElasticsearchOperationClassifier.BuildUri(op, ElasticsearchTrackingVerbosity.Detailed);
        Assert.Equal("elasticsearch:///orders", uri.ToString());
    }

    [Fact]
    public void BuildUri_Summarised_uses_base_uri()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.Search, "orders");
        var uri = ElasticsearchOperationClassifier.BuildUri(op, ElasticsearchTrackingVerbosity.Summarised);
        Assert.Equal("elasticsearch:///", uri.ToString());
    }

    [Fact]
    public void BuildUri_Raw_uses_raw_uri()
    {
        var rawUri = new Uri("http://localhost:9200/orders/_search?q=*");
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.Search, "orders");
        var uri = ElasticsearchOperationClassifier.BuildUri(op, ElasticsearchTrackingVerbosity.Raw, rawUri);
        Assert.Equal(rawUri, uri);
    }

    [Fact]
    public void BuildUri_Detailed_without_index_uses_cluster()
    {
        var op = new ElasticsearchOperationInfo(ElasticsearchOperation.ClusterHealth, null);
        var uri = ElasticsearchOperationClassifier.BuildUri(op, ElasticsearchTrackingVerbosity.Detailed);
        Assert.Equal("elasticsearch:///cluster", uri.ToString());
    }

    // ─── Index names with special characters ────────────────────

    [Fact]
    public void Hyphenated_index_name_classified_correctly()
    {
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Post, MakeUri("/my-orders/_search"));
        Assert.Equal(ElasticsearchOperation.Search, result.Operation);
        Assert.Equal("my-orders", result.IndexName);
    }

    [Fact]
    public void Underscore_prefix_index_not_confused_with_system_path()
    {
        // Paths starting with /_ are system paths, not indices
        var result = ElasticsearchOperationClassifier.Classify(HttpMethod.Get, MakeUri("/_something"));
        Assert.Equal(ElasticsearchOperation.Other, result.Operation);
    }
}
