using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.S3;

public static partial class S3OperationClassifier
{
    // Path-style: https://s3.region.amazonaws.com/{bucket}/{key}
    [GeneratedRegex(
        @"^/(?<bucket>[^/?]+)(?:/(?<key>.+?))?(?:\?.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PathStyleRegex();

    // Virtual-hosted-style: https://{bucket}.s3[.-]{region}.amazonaws.com/{key}
    [GeneratedRegex(
        @"^(?<bucket>[^.]+)\.s3[\.-]",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex VirtualHostedRegex();

    public static S3OperationInfo Classify(HttpRequestMessage request)
    {
        var uri = request.RequestUri;
        if (uri is null)
            return new S3OperationInfo(S3Operation.Other, null);

        var host = uri.Host;
        var path = uri.AbsolutePath;
        var query = uri.Query;
        var method = request.Method.Method.ToUpperInvariant();

        string? bucket;
        string? key;

        var virtualMatch = VirtualHostedRegex().Match(host);
        if (virtualMatch.Success)
        {
            // Virtual-hosted-style: bucket is in the host
            bucket = virtualMatch.Groups["bucket"].Value;
            var pathMatch = PathStyleRegex().Match(path);
            // In virtual-hosted, the path IS the key (no bucket prefix)
            key = path.Length > 1 ? path[1..] : null; // strip leading /
            if (string.IsNullOrEmpty(key)) key = null;
        }
        else
        {
            // Path-style: bucket and key from path
            var pathMatch = PathStyleRegex().Match(path);
            if (!pathMatch.Success || string.IsNullOrEmpty(pathMatch.Groups["bucket"].Value))
                return new S3OperationInfo(S3Operation.ListBuckets, null);

            bucket = pathMatch.Groups["bucket"].Value;
            key = NullIfEmpty(pathMatch.Groups["key"].Value);
        }

        var queryParams = ParseQueryString(query);
        var hasCopySource = request.Headers.Contains("x-amz-copy-source");
        var hasKey = key is not null;

        var operation = (method, hasKey, hasCopySource, queryParams) switch
        {
            // Copy (must check before PutObject)
            ("PUT", true, true, _) => S3Operation.CopyObject,

            // Multipart upload parts
            ("PUT", true, false, var q) when q.ContainsKey("partNumber") && q.ContainsKey("uploadId")
                => S3Operation.UploadPart,

            // Tagging operations on objects
            ("PUT", true, false, var q) when q.ContainsKey("tagging") => S3Operation.PutObjectTagging,
            ("GET", true, _, var q) when q.ContainsKey("tagging") => S3Operation.GetObjectTagging,
            ("DELETE", true, _, var q) when q.ContainsKey("tagging") => S3Operation.DeleteObjectTagging,

            // Multipart initiate/complete/abort
            ("POST", true, _, var q) when q.ContainsKey("uploads") => S3Operation.CreateMultipartUpload,
            ("POST", true, _, var q) when q.ContainsKey("uploadId") => S3Operation.CompleteMultipartUpload,
            ("DELETE", true, _, var q) when q.ContainsKey("uploadId") => S3Operation.AbortMultipartUpload,

            // Standard object CRUD
            ("PUT", true, false, _) => S3Operation.PutObject,
            ("GET", true, _, _) => S3Operation.GetObject,
            ("DELETE", true, _, _) => S3Operation.DeleteObject,
            ("HEAD", true, _, _) => S3Operation.HeadObject,

            // Bucket-level operations (no key)
            ("GET", false, _, var q) when q.ContainsKey("list-type") => S3Operation.ListObjectsV2,
            ("GET", false, _, var q) when q.ContainsKey("versions") => S3Operation.ListObjectVersions,
            ("GET", false, _, var q) when q.ContainsKey("location") => S3Operation.GetBucketLocation,
            ("POST", false, _, var q) when q.ContainsKey("delete") => S3Operation.DeleteObjects,
            ("PUT", false, _, _) => S3Operation.CreateBucket,
            ("DELETE", false, _, _) => S3Operation.DeleteBucket,

            _ => S3Operation.Other
        };

        return new S3OperationInfo(operation, NullIfEmpty(bucket), key);
    }

    public static string? GetDiagramLabel(S3OperationInfo op, S3TrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            S3TrackingVerbosity.Summarised or S3TrackingVerbosity.Detailed => op.Operation.ToString(),
            _ => null // Raw: caller uses HTTP method + path
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
                var pairKey = Uri.UnescapeDataString(pair[..eqIndex]);
                var value = Uri.UnescapeDataString(pair[(eqIndex + 1)..]);
                result[pairKey] = value;
            }
        }
        return result;
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;
}
