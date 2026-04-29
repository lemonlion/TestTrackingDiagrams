using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.BigQuery;

/// <summary>
/// Classifies Google BigQuery HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class BigQueryOperationClassifier
{
    // Matches BigQuery REST API paths, including the optional /upload/ prefix.
    // /bigquery/v2/projects/{project}/{resource-path}
    // /upload/bigquery/v2/projects/{project}/{resource-path}
    [GeneratedRegex(
        @"^/(?:upload/)?bigquery/v2/projects/(?<project>[^/]+)/(?<rest>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BigQueryPathRegex();

    public static BigQueryOperationInfo Classify(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var method = request.Method.Method.ToUpperInvariant();

        var match = BigQueryPathRegex().Match(path);
        if (!match.Success)
            return new BigQueryOperationInfo(BigQueryOperation.Other, null, null, null, null);

        var project = match.Groups["project"].Value;
        var rest = match.Groups["rest"].Value;

        // Strip trailing query string remnants and split into segments
        var segments = rest.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return ClassifyFromSegments(method, segments, project);
    }

    public static string? GetDiagramLabel(BigQueryOperationInfo op, BigQueryTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            BigQueryTrackingVerbosity.Summarised or BigQueryTrackingVerbosity.Detailed => op.Operation.ToString(),
            _ => null
        };
    }

    private static BigQueryOperationInfo ClassifyFromSegments(string method, string[] segments, string project)
    {
        if (segments.Length == 0)
            return new BigQueryOperationInfo(BigQueryOperation.Other, null, null, project, null);

        return segments[0].ToLowerInvariant() switch
        {
            "queries" => ClassifyQuery(method, segments, project),
            "datasets" => ClassifyDataset(method, segments, project),
            "jobs" => ClassifyJob(method, segments, project),
            _ => new BigQueryOperationInfo(BigQueryOperation.Other, null, null, project, null)
        };
    }

    private static BigQueryOperationInfo ClassifyQuery(string method, string[] segments, string project)
    {
        // POST /queries → run query
        // GET  /queries/{jobId} → get query results
        var resourceName = segments.Length > 1 ? segments[1] : null;
        return new BigQueryOperationInfo(BigQueryOperation.Query, "query", resourceName, project, null);
    }

    private static BigQueryOperationInfo ClassifyDataset(string method, string[] segments, string project)
    {
        // /datasets → list or create
        if (segments.Length == 1)
        {
            var op = method == "POST" ? BigQueryOperation.Create : BigQueryOperation.List;
            return new BigQueryOperationInfo(op, "dataset", null, project, null);
        }

        var datasetId = segments[1];

        // /datasets/{id} → CRUD on dataset itself
        if (segments.Length == 2)
        {
            var op = method switch
            {
                "DELETE" => BigQueryOperation.Delete,
                "PUT" or "PATCH" => BigQueryOperation.Update,
                _ => BigQueryOperation.Read
            };
            return new BigQueryOperationInfo(op, "dataset", datasetId, project, datasetId);
        }

        // /datasets/{id}/{sub-resource}/...
        return segments[2].ToLowerInvariant() switch
        {
            "tables" => ClassifyTable(method, segments, project, datasetId),
            "models" => ClassifySubResource(method, segments, project, datasetId, "model", 3),
            "routines" => ClassifySubResource(method, segments, project, datasetId, "routine", 3),
            _ => new BigQueryOperationInfo(BigQueryOperation.Other, null, null, project, datasetId)
        };
    }

    private static BigQueryOperationInfo ClassifyTable(string method, string[] segments, string project, string datasetId)
    {
        // /datasets/{ds}/tables → list or create
        if (segments.Length == 3)
        {
            var op = method == "POST" ? BigQueryOperation.Create : BigQueryOperation.List;
            return new BigQueryOperationInfo(op, "table", null, project, datasetId);
        }

        var tableId = segments[3];

        // /datasets/{ds}/tables/{tbl}/insertAll
        if (segments.Length > 4 && segments[4].Equals("insertAll", StringComparison.OrdinalIgnoreCase))
            return new BigQueryOperationInfo(BigQueryOperation.Insert, "table", tableId, project, datasetId);

        // /datasets/{ds}/tables/{tbl}/data
        if (segments.Length > 4 && segments[4].Equals("data", StringComparison.OrdinalIgnoreCase))
            return new BigQueryOperationInfo(BigQueryOperation.List, "tabledata", tableId, project, datasetId);

        // /datasets/{ds}/tables/{tbl} → CRUD
        var tableOp = method switch
        {
            "DELETE" => BigQueryOperation.Delete,
            "PUT" or "PATCH" => BigQueryOperation.Update,
            _ => BigQueryOperation.Read
        };
        return new BigQueryOperationInfo(tableOp, "table", tableId, project, datasetId);
    }

    private static BigQueryOperationInfo ClassifySubResource(
        string method, string[] segments, string project, string datasetId,
        string resourceType, int resourceIndex)
    {
        // /datasets/{ds}/{type} → list
        if (segments.Length <= resourceIndex)
        {
            var op = method == "POST" ? BigQueryOperation.Create : BigQueryOperation.List;
            return new BigQueryOperationInfo(op, resourceType, null, project, datasetId);
        }

        var resourceName = segments[resourceIndex];

        var subOp = method switch
        {
            "DELETE" => BigQueryOperation.Delete,
            "PUT" or "PATCH" => BigQueryOperation.Update,
            "POST" => BigQueryOperation.Create,
            _ => BigQueryOperation.Read
        };
        return new BigQueryOperationInfo(subOp, resourceType, resourceName, project, datasetId);
    }

    private static BigQueryOperationInfo ClassifyJob(string method, string[] segments, string project)
    {
        // /jobs → list or create
        if (segments.Length == 1)
        {
            var op = method == "POST" ? BigQueryOperation.Create : BigQueryOperation.List;
            return new BigQueryOperationInfo(op, "job", null, project, null);
        }

        var jobId = segments[1];

        // /jobs/{id}/cancel
        if (segments.Length > 2 && segments[2].Equals("cancel", StringComparison.OrdinalIgnoreCase))
            return new BigQueryOperationInfo(BigQueryOperation.Cancel, "job", jobId, project, null);

        // /jobs/{id}/delete
        if (segments.Length > 2 && segments[2].Equals("delete", StringComparison.OrdinalIgnoreCase))
            return new BigQueryOperationInfo(BigQueryOperation.Delete, "job", jobId, project, null);

        // /jobs/{id} → read or delete
        var jobOp = method switch
        {
            "DELETE" => BigQueryOperation.Delete,
            _ => BigQueryOperation.Read
        };
        return new BigQueryOperationInfo(jobOp, "job", jobId, project, null);
    }
}