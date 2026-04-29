using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.Elasticsearch;

/// <summary>
/// Classifies Elasticsearch HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class ElasticsearchOperationClassifier
{
    [GeneratedRegex(@"^/(?<index>[^/_][^/]*)/_doc/(?<id>[^/?]+)")]
    private static partial Regex DocWithIdRegex();

    [GeneratedRegex(@"^/(?<index>[^/_][^/]*)/_doc/?$")]
    private static partial Regex DocWithoutIdRegex();

    [GeneratedRegex(@"^/(?<index>[^/_][^/]*)/_update/(?<id>[^/?]+)")]
    private static partial Regex UpdateDocRegex();

    [GeneratedRegex(@"^/(?<index>[^/_][^/]*)/_(?<action>search|count|bulk|delete_by_query|update_by_query|mapping|refresh)/?")]
    private static partial Regex IndexActionRegex();

    [GeneratedRegex(@"^/(?<index>[^/_][^/]*)/?$")]
    private static partial Regex IndexOnlyRegex();

    public static ElasticsearchOperationInfo Classify(HttpMethod method, Uri uri)
    {
        var path = uri.AbsolutePath;

        // Cluster-level operations
        if (path.StartsWith("/_cluster/health"))
            return new(ElasticsearchOperation.ClusterHealth, null);
        if (path.StartsWith("/_cat/"))
            return new(ElasticsearchOperation.CatApis, null);
        if (path.StartsWith("/_msearch"))
            return new(ElasticsearchOperation.MultiSearch, null);
        if (path == "/_bulk")
            return new(ElasticsearchOperation.Bulk, null);
        if (path.StartsWith("/_index_template/"))
            return new(method == HttpMethod.Put
                ? ElasticsearchOperation.PutIndexTemplate
                : ElasticsearchOperation.GetIndexTemplate, null);
        if (path == "/_aliases")
            return new(ElasticsearchOperation.UpdateAliases, null);
        if (path == "/_reindex")
            return new(ElasticsearchOperation.Reindex, null);
        if (path.Contains("/_scroll") || path.Contains("/_search/scroll"))
            return new(ElasticsearchOperation.Scroll, null);

        // Document with ID: /{index}/_doc/{id}
        var docMatch = DocWithIdRegex().Match(path);
        if (docMatch.Success)
        {
            var index = docMatch.Groups["index"].Value;
            var id = docMatch.Groups["id"].Value;
            return method.Method switch
            {
                "GET" => new(ElasticsearchOperation.GetDocument, index, id),
                "DELETE" => new(ElasticsearchOperation.DeleteDocument, index, id),
                "PUT" => new(ElasticsearchOperation.IndexDocument, index, id),
                _ => new(ElasticsearchOperation.Other, index, id)
            };
        }

        // Document without ID: POST /{index}/_doc
        var docNoIdMatch = DocWithoutIdRegex().Match(path);
        if (docNoIdMatch.Success)
            return new(ElasticsearchOperation.IndexDocument, docNoIdMatch.Groups["index"].Value);

        // /{index}/_update/{id}
        var updateMatch = UpdateDocRegex().Match(path);
        if (updateMatch.Success)
            return new(ElasticsearchOperation.UpdateDocument,
                updateMatch.Groups["index"].Value, updateMatch.Groups["id"].Value);

        // Index-level actions: /{index}/_search, /_count, /_bulk, etc.
        var actionMatch = IndexActionRegex().Match(path);
        if (actionMatch.Success)
        {
            var index = actionMatch.Groups["index"].Value;
            var action = actionMatch.Groups["action"].Value;
            return action switch
            {
                "search" => new(ElasticsearchOperation.Search, index),
                "count" => new(ElasticsearchOperation.Count, index),
                "bulk" => new(ElasticsearchOperation.Bulk, index),
                "delete_by_query" => new(ElasticsearchOperation.DeleteByQuery, index),
                "update_by_query" => new(ElasticsearchOperation.UpdateByQuery, index),
                "mapping" when method == HttpMethod.Put => new(ElasticsearchOperation.PutMapping, index),
                "mapping" when method == HttpMethod.Get => new(ElasticsearchOperation.GetMapping, index),
                "refresh" => new(ElasticsearchOperation.Refresh, index),
                _ => new(ElasticsearchOperation.Other, index)
            };
        }

        // Index-only: PUT/DELETE/HEAD /{index}
        var indexMatch = IndexOnlyRegex().Match(path);
        if (indexMatch.Success)
        {
            var index = indexMatch.Groups["index"].Value;
            return method.Method switch
            {
                "PUT" => new(ElasticsearchOperation.CreateIndex, index),
                "DELETE" => new(ElasticsearchOperation.DeleteIndex, index),
                "HEAD" => new(ElasticsearchOperation.IndexExists, index),
                "GET" => new(ElasticsearchOperation.GetMapping, index),
                _ => new(ElasticsearchOperation.Other, index)
            };
        }

        return new(ElasticsearchOperation.Other, null);
    }

    public static string GetDiagramLabel(ElasticsearchOperationInfo op, ElasticsearchTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            ElasticsearchTrackingVerbosity.Raw =>
                $"{op.Operation} /{op.IndexName}" + (op.DocumentId is not null ? $"/{op.DocumentId}" : ""),
            ElasticsearchTrackingVerbosity.Detailed => op.Operation switch
            {
                ElasticsearchOperation.IndexDocument => $"Index → {op.IndexName}",
                ElasticsearchOperation.GetDocument => $"Get ← {op.IndexName}",
                ElasticsearchOperation.DeleteDocument => $"Delete {op.IndexName}",
                ElasticsearchOperation.UpdateDocument => $"Update {op.IndexName}",
                ElasticsearchOperation.Search => $"Search → {op.IndexName}",
                ElasticsearchOperation.MultiSearch => "MultiSearch",
                ElasticsearchOperation.Count => $"Count {op.IndexName}",
                ElasticsearchOperation.Bulk => $"Bulk → {op.IndexName ?? "multi"}",
                ElasticsearchOperation.CreateIndex => $"CreateIndex {op.IndexName}",
                ElasticsearchOperation.DeleteIndex => $"DeleteIndex {op.IndexName}",
                _ => op.Operation.ToString()
            },
            ElasticsearchTrackingVerbosity.Summarised => op.Operation switch
            {
                ElasticsearchOperation.IndexDocument => "Index",
                ElasticsearchOperation.GetDocument => "Get",
                ElasticsearchOperation.DeleteDocument => "Delete",
                ElasticsearchOperation.UpdateDocument => "Update",
                ElasticsearchOperation.Search => "Search",
                ElasticsearchOperation.Bulk => "Bulk",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }

    public static Uri BuildUri(ElasticsearchOperationInfo op, ElasticsearchTrackingVerbosity verbosity, Uri? rawUri = null)
    {
        return verbosity switch
        {
            ElasticsearchTrackingVerbosity.Raw when rawUri is not null => rawUri,
            ElasticsearchTrackingVerbosity.Detailed =>
                new Uri($"elasticsearch:///{op.IndexName ?? "cluster"}"),
            ElasticsearchTrackingVerbosity.Summarised =>
                new Uri("elasticsearch:///"),
            _ => new Uri($"elasticsearch:///{op.IndexName ?? "cluster"}")
        };
    }
}