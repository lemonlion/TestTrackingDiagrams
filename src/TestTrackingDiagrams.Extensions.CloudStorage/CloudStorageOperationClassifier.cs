using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.CloudStorage;

/// <summary>
/// Classifies Google Cloud Storage HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class CloudStorageOperationClassifier
{
    // Match /storage/v1/b/{bucket}/o/{object} or /upload/storage/v1/b/{bucket}/o
    [GeneratedRegex(@"/(?:upload/)?storage/v1/b/(?<bucket>[^/?]+)(?:/o(?:/(?<object>[^/?]+))?)?", RegexOptions.Compiled)]
    private static partial Regex GcsPathRegex();

    // Match bucket-only: /storage/v1/b or /storage/v1/b/{bucket}
    [GeneratedRegex(@"/storage/v1/b(?:/(?<bucket>[^/?]+))?$", RegexOptions.Compiled)]
    private static partial Regex BucketOnlyRegex();

    public static CloudStorageOperationInfo Classify(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var method = request.Method;

        var isUpload = path.StartsWith("/upload/");

        var match = GcsPathRegex().Match(path);
        if (!match.Success)
        {
            var bucketMatch = BucketOnlyRegex().Match(path);
            if (bucketMatch.Success)
            {
                var bkt = bucketMatch.Groups["bucket"].Success ? bucketMatch.Groups["bucket"].Value : null;
                var hasBucket = !string.IsNullOrEmpty(bkt);
                return (method.Method, hasBucket) switch
                {
                    ("GET", true) => new(CloudStorageOperation.GetBucket, bkt),
                    ("DELETE", true) => new(CloudStorageOperation.DeleteBucket, bkt),
                    ("POST", _) => new(CloudStorageOperation.CreateBucket, null),
                    ("GET", false) => new(CloudStorageOperation.ListBuckets, null),
                    _ => new(CloudStorageOperation.Other, null)
                };
            }
            return new(CloudStorageOperation.Other, null);
        }

        var bucket = match.Groups["bucket"].Value;

        // If the path doesn't contain /o after the bucket, it's a bucket-level operation
        var fullMatch = match.Value;
        if (!fullMatch.Contains("/o"))
        {
            return (method.Method) switch
            {
                "GET" => new(CloudStorageOperation.GetBucket, bucket),
                "DELETE" => new(CloudStorageOperation.DeleteBucket, bucket),
                _ => new(CloudStorageOperation.Other, bucket)
            };
        }

        var obj = match.Groups["object"].Success && match.Groups["object"].Value.Length > 0
            ? Uri.UnescapeDataString(match.Groups["object"].Value)
            : null;
        var hasObject = obj is not null;

        if (path.Contains("/copyTo/")) return new(CloudStorageOperation.Copy, bucket, obj);
        if (path.Contains("/compose")) return new(CloudStorageOperation.Compose, bucket, obj);

        return (method.Method, hasObject, isUpload) switch
        {
            ("POST", _, true) => new(CloudStorageOperation.Upload, bucket, obj),
            ("PUT", true, _) => new(CloudStorageOperation.Upload, bucket, obj),
            ("GET", true, _) when HasAltMediaParam(request) => new(CloudStorageOperation.Download, bucket, obj),
            ("GET", true, _) => new(CloudStorageOperation.GetMetadata, bucket, obj),
            ("DELETE", true, _) => new(CloudStorageOperation.Delete, bucket, obj),
            ("PATCH", true, _) => new(CloudStorageOperation.UpdateMetadata, bucket, obj),
            ("GET", false, _) => new(CloudStorageOperation.ListObjects, bucket),
            _ => new(CloudStorageOperation.Other, bucket, obj)
        };
    }

    private static bool HasAltMediaParam(HttpRequestMessage request) =>
        request.RequestUri?.Query.Contains("alt=media") == true;

    public static string GetDiagramLabel(CloudStorageOperationInfo op, CloudStorageTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            CloudStorageTrackingVerbosity.Detailed => op.ObjectName is not null
                ? $"{op.Operation} → {op.BucketName}/{op.ObjectName}"
                : op.BucketName is not null
                    ? $"{op.Operation} → {op.BucketName}"
                    : op.Operation.ToString(),
            CloudStorageTrackingVerbosity.Summarised => op.Operation.ToString(),
            _ => op.Operation.ToString() // Raw uses HTTP method
        };
    }
}