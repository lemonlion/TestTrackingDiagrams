using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.BlobStorage;

/// <summary>
/// Classifies Azure Blob Storage HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class BlobOperationClassifier
{
    // Azure Blob Storage REST API paths:
    //   /{container}?restype=container              → container operations
    //   /{container}?restype=container&comp=list     → list blobs
    //   /{container}/{blob}                          → blob operations
    //   /{container}/{blob}?comp=metadata            → metadata operations
    //   /{container}/{blob}?comp=copy                → copy
    //   /{container}/{blob}?comp=block&blockid=...   → put block
    //   /{container}/{blob}?comp=blocklist            → put block list
    //   /{container}/{blob}?comp=lease               → lease
    [GeneratedRegex(
        @"^/(?<container>[^/?]+)(?:/(?<blob>[^?]+))?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BlobPathRegex();

    public static BlobOperationInfo Classify(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var query = request.RequestUri?.Query ?? "";
        var method = request.Method.Method.ToUpperInvariant();

        var match = BlobPathRegex().Match(path);
        var container = match.Groups["container"].Value;
        var blob = match.Groups["blob"].Value;

        var hasBlob = !string.IsNullOrEmpty(blob);
        var queryParams = ParseQueryString(query);
        var isContainer = queryParams.ContainsKey("restype") &&
                          queryParams["restype"].Equals("container", StringComparison.OrdinalIgnoreCase);
        var comp = queryParams.GetValueOrDefault("comp")?.ToLowerInvariant();

        var operation = (method, hasBlob, isContainer, comp) switch
        {
            // Container operations
            ("PUT", false, true, null or "") => BlobOperation.CreateContainer,
            ("DELETE", false, true, _) => BlobOperation.DeleteContainer,
            (_, false, true, "list") => BlobOperation.ListBlobs,

            // Blob metadata
            ("PUT", true, _, "metadata") => BlobOperation.SetMetadata,
            ("GET", true, _, "metadata") => BlobOperation.GetMetadata,

            // Blob composition
            ("PUT", true, _, "copy") => BlobOperation.Copy,
            ("PUT", true, _, "block") => BlobOperation.PutBlock,
            ("PUT", true, _, "blocklist") => BlobOperation.PutBlockList,

            // Lease
            (_, true, _, "lease") => BlobOperation.Lease,

            // Standard blob CRUD
            ("PUT", true, _, null or "") => BlobOperation.Upload,
            ("GET", true, _, null or "") => BlobOperation.Download,
            ("DELETE", true, _, null or "") => BlobOperation.Delete,
            ("HEAD", true, _, null or "") => BlobOperation.GetProperties,

            _ => BlobOperation.Other
        };

        return new BlobOperationInfo(
            operation,
            NullIfEmpty(container),
            hasBlob ? blob : null);
    }

    public static string GetDiagramLabel(BlobOperationInfo op, BlobTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            BlobTrackingVerbosity.Summarised or BlobTrackingVerbosity.Detailed => op.Operation.ToString(),
            _ => null!
        };
    }

    private static Dictionary<string, string> ParseQueryString(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query)) return result;

        var q = query.TrimStart('?');
        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eqIndex = pair.IndexOf('=');
            if (eqIndex < 0)
            {
                result[Uri.UnescapeDataString(pair)] = "";
            }
            else
            {
                var key = Uri.UnescapeDataString(pair[..eqIndex]);
                var value = Uri.UnescapeDataString(pair[(eqIndex + 1)..]);
                result[key] = value;
            }
        }
        return result;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
}