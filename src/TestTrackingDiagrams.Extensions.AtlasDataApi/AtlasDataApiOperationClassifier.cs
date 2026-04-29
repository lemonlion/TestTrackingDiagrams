using System.Text.Json;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

/// <summary>
/// Classifies MongoDB Atlas Data API HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class AtlasDataApiOperationClassifier
{
    // Matches Atlas Data API action endpoint:
    // /app/{appId}/endpoint/data/v1/action/{actionName}
    // or /api/client/v2.0/app/{appId}/endpoint/data/v1/action/{actionName}
    [GeneratedRegex(
        @"/action/(?<action>[a-zA-Z]+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ActionPathRegex();

    public static AtlasDataApiOperationInfo Classify(HttpRequestMessage request, string? bodyJson = null)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";

        var match = ActionPathRegex().Match(path);
        if (!match.Success)
            return new AtlasDataApiOperationInfo(AtlasDataApiOperation.Other, null, null, null);

        var actionName = match.Groups["action"].Value;

        var operation = actionName.ToLowerInvariant() switch
        {
            "findone" => AtlasDataApiOperation.FindOne,
            "find" => AtlasDataApiOperation.Find,
            "insertone" => AtlasDataApiOperation.InsertOne,
            "insertmany" => AtlasDataApiOperation.InsertMany,
            "updateone" => AtlasDataApiOperation.UpdateOne,
            "updatemany" => AtlasDataApiOperation.UpdateMany,
            "deleteone" => AtlasDataApiOperation.DeleteOne,
            "deletemany" => AtlasDataApiOperation.DeleteMany,
            "replaceone" => AtlasDataApiOperation.ReplaceOne,
            "aggregate" => AtlasDataApiOperation.Aggregate,
            _ => AtlasDataApiOperation.Other
        };

        string? dataSource = null;
        string? database = null;
        string? collection = null;
        string? filterText = null;

        if (bodyJson is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(bodyJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("dataSource", out var ds))
                    dataSource = ds.GetString();
                if (root.TryGetProperty("database", out var db))
                    database = db.GetString();
                if (root.TryGetProperty("collection", out var col))
                    collection = col.GetString();
                if (root.TryGetProperty("filter", out var filter))
                    filterText = filter.ToString();
            }
            catch (JsonException)
            {
                // Malformed JSON — skip extraction
            }
        }

        return new AtlasDataApiOperationInfo(operation, dataSource, database, collection, filterText);
    }

    public static string GetDiagramLabel(AtlasDataApiOperationInfo op, AtlasDataApiTrackingVerbosity verbosity)
    {
        var arrow = GetDirectionalArrow(op.Operation);

        return verbosity switch
        {
            AtlasDataApiTrackingVerbosity.Detailed when op.CollectionName is not null =>
                $"{op.Operation} {arrow} {op.CollectionName}",
            AtlasDataApiTrackingVerbosity.Detailed => op.Operation.ToString(),
            AtlasDataApiTrackingVerbosity.Summarised => op.Operation.ToString(),
            _ => op.Operation.ToString() // Raw uses HTTP method
        };
    }

    internal static string GetDirectionalArrow(AtlasDataApiOperation operation)
    {
        return operation switch
        {
            AtlasDataApiOperation.FindOne or AtlasDataApiOperation.Find or AtlasDataApiOperation.Aggregate =>
                "←",
            AtlasDataApiOperation.InsertOne or AtlasDataApiOperation.InsertMany or AtlasDataApiOperation.DeleteOne
                or AtlasDataApiOperation.DeleteMany =>
                "→",
            AtlasDataApiOperation.UpdateOne or AtlasDataApiOperation.UpdateMany or AtlasDataApiOperation.ReplaceOne =>
                "↔",
            _ => "→"
        };
    }
}