using TestTrackingDiagrams.Extensions.S3;

namespace TestTrackingDiagrams.Tests.S3;

public class S3OperationClassifierTests
{
    // ══════════════════════════════════════════════════════════
    //  Path-style URL tests
    //  Format: https://s3.{region}.amazonaws.com/{bucket}/{key}
    // ══════════════════════════════════════════════════════════

    // ─── Standard Object CRUD ─────────────────────────────────

    [Fact]
    public void Classify_PutObject_PathStyle_ReturnsPutObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://s3.us-east-1.amazonaws.com/my-bucket/path/to/file.json");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.PutObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("path/to/file.json", result.KeyName);
    }

    [Fact]
    public void Classify_GetObject_PathStyle_ReturnsGetObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket/document.pdf");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("document.pdf", result.KeyName);
    }

    [Fact]
    public void Classify_DeleteObject_PathStyle_ReturnsDeleteObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://s3.us-east-1.amazonaws.com/my-bucket/old-file.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("old-file.txt", result.KeyName);
    }

    [Fact]
    public void Classify_HeadObject_PathStyle_ReturnsHeadObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Head,
            "https://s3.us-east-1.amazonaws.com/my-bucket/check-exists.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.HeadObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("check-exists.txt", result.KeyName);
    }

    // ─── Copy Operation ───────────────────────────────────────

    [Fact]
    public void Classify_CopyObject_PathStyle_ReturnsCopyObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://s3.us-east-1.amazonaws.com/dest-bucket/dest-key.txt");
        request.Headers.Add("x-amz-copy-source", "/source-bucket/source-key.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CopyObject, result.Operation);
        Assert.Equal("dest-bucket", result.BucketName);
        Assert.Equal("dest-key.txt", result.KeyName);
    }

    // ─── Bucket Operations ────────────────────────────────────

    [Fact]
    public void Classify_ListObjectsV2_PathStyle_ReturnsListObjectsV2()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket?list-type=2");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.ListObjectsV2, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_CreateBucket_PathStyle_ReturnsCreateBucket()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://s3.us-east-1.amazonaws.com/new-bucket");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CreateBucket, result.Operation);
        Assert.Equal("new-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_DeleteBucket_PathStyle_ReturnsDeleteBucket()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://s3.us-east-1.amazonaws.com/old-bucket");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteBucket, result.Operation);
        Assert.Equal("old-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_DeleteObjects_PathStyle_ReturnsDeleteObjects()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://s3.us-east-1.amazonaws.com/my-bucket?delete");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteObjects, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_ListBuckets_PathStyle_ReturnsListBuckets()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.ListBuckets, result.Operation);
        Assert.Null(result.BucketName);
    }

    [Fact]
    public void Classify_GetBucketLocation_PathStyle_ReturnsGetBucketLocation()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket?location");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetBucketLocation, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
    }

    [Fact]
    public void Classify_ListObjectVersions_PathStyle_ReturnsListObjectVersions()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket?versions");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.ListObjectVersions, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
    }

    // ─── Multipart Operations ─────────────────────────────────

    [Fact]
    public void Classify_CreateMultipartUpload_PathStyle_ReturnsCreateMultipartUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://s3.us-east-1.amazonaws.com/my-bucket/large-file.zip?uploads");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CreateMultipartUpload, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("large-file.zip", result.KeyName);
    }

    [Fact]
    public void Classify_UploadPart_PathStyle_ReturnsUploadPart()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://s3.us-east-1.amazonaws.com/my-bucket/large-file.zip?partNumber=1&uploadId=abc123");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.UploadPart, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("large-file.zip", result.KeyName);
    }

    [Fact]
    public void Classify_CompleteMultipartUpload_PathStyle_ReturnsCompleteMultipartUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://s3.us-east-1.amazonaws.com/my-bucket/large-file.zip?uploadId=abc123");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CompleteMultipartUpload, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("large-file.zip", result.KeyName);
    }

    [Fact]
    public void Classify_AbortMultipartUpload_PathStyle_ReturnsAbortMultipartUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://s3.us-east-1.amazonaws.com/my-bucket/large-file.zip?uploadId=abc123");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.AbortMultipartUpload, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("large-file.zip", result.KeyName);
    }

    // ─── Tagging Operations ───────────────────────────────────

    [Fact]
    public void Classify_PutObjectTagging_PathStyle_ReturnsPutObjectTagging()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://s3.us-east-1.amazonaws.com/my-bucket/file.txt?tagging");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.PutObjectTagging, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("file.txt", result.KeyName);
    }

    [Fact]
    public void Classify_GetObjectTagging_PathStyle_ReturnsGetObjectTagging()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket/file.txt?tagging");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObjectTagging, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("file.txt", result.KeyName);
    }

    [Fact]
    public void Classify_DeleteObjectTagging_PathStyle_ReturnsDeleteObjectTagging()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://s3.us-east-1.amazonaws.com/my-bucket/file.txt?tagging");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteObjectTagging, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("file.txt", result.KeyName);
    }

    // ══════════════════════════════════════════════════════════
    //  Virtual-hosted-style URL tests
    //  Format: https://{bucket}.s3.{region}.amazonaws.com/{key}
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void Classify_PutObject_VirtualHosted_ReturnsPutObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://my-bucket.s3.us-east-1.amazonaws.com/path/to/file.json");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.PutObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("path/to/file.json", result.KeyName);
    }

    [Fact]
    public void Classify_GetObject_VirtualHosted_ReturnsGetObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://my-bucket.s3.us-east-1.amazonaws.com/document.pdf");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("document.pdf", result.KeyName);
    }

    [Fact]
    public void Classify_DeleteObject_VirtualHosted_ReturnsDeleteObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://my-bucket.s3.us-east-1.amazonaws.com/old-file.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("old-file.txt", result.KeyName);
    }

    [Fact]
    public void Classify_HeadObject_VirtualHosted_ReturnsHeadObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Head,
            "https://my-bucket.s3.us-east-1.amazonaws.com/check.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.HeadObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
    }

    [Fact]
    public void Classify_ListObjectsV2_VirtualHosted_ReturnsListObjectsV2()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://my-bucket.s3.us-east-1.amazonaws.com/?list-type=2");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.ListObjectsV2, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_CopyObject_VirtualHosted_ReturnsCopyObject()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://dest-bucket.s3.us-east-1.amazonaws.com/dest-key.txt");
        request.Headers.Add("x-amz-copy-source", "/source-bucket/source-key.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CopyObject, result.Operation);
        Assert.Equal("dest-bucket", result.BucketName);
        Assert.Equal("dest-key.txt", result.KeyName);
    }

    [Fact]
    public void Classify_CreateMultipartUpload_VirtualHosted_ReturnsCreateMultipartUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://my-bucket.s3.us-east-1.amazonaws.com/large-file.zip?uploads");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CreateMultipartUpload, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("large-file.zip", result.KeyName);
    }

    [Fact]
    public void Classify_DeleteObjects_VirtualHosted_ReturnsDeleteObjects()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://my-bucket.s3.us-east-1.amazonaws.com/?delete");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteObjects, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
    }

    // ─── Virtual-hosted legacy formats ────────────────────────

    [Fact]
    public void Classify_VirtualHosted_LegacyGlobalEndpoint_ReturnsCorrectBucket()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://my-bucket.s3.amazonaws.com/file.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("file.txt", result.KeyName);
    }

    [Fact]
    public void Classify_VirtualHosted_LegacyDashRegion_ReturnsCorrectBucket()
    {
        // Legacy: bucket.s3-region.amazonaws.com
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://my-bucket.s3-us-west-2.amazonaws.com/file.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("file.txt", result.KeyName);
    }

    // ══════════════════════════════════════════════════════════
    //  Edge cases
    // ══════════════════════════════════════════════════════════

    [Fact]
    public void Classify_NestedKeyPath_ParsesCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket/a/b/c/d/file.json");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("a/b/c/d/file.json", result.KeyName);
    }

    [Fact]
    public void Classify_NullRequestUri_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, (Uri?)null);

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UnrecognisedOperation_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch,
            "https://s3.us-east-1.amazonaws.com/my-bucket/file.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UrlEncodedKey_ParsesCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://s3.us-east-1.amazonaws.com/my-bucket/path%20with%20spaces/file%20name.txt");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetObject, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("path%20with%20spaces/file%20name.txt", result.KeyName);
    }

    [Fact]
    public void Classify_CreateBucket_VirtualHosted_ReturnsCreateBucket()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://new-bucket.s3.us-east-1.amazonaws.com/");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CreateBucket, result.Operation);
        Assert.Equal("new-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_DeleteBucket_VirtualHosted_ReturnsDeleteBucket()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://old-bucket.s3.us-east-1.amazonaws.com/");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteBucket, result.Operation);
        Assert.Equal("old-bucket", result.BucketName);
        Assert.Null(result.KeyName);
    }

    [Fact]
    public void Classify_GetBucketLocation_VirtualHosted_ReturnsGetBucketLocation()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://my-bucket.s3.us-east-1.amazonaws.com/?location");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.GetBucketLocation, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
    }

    [Fact]
    public void Classify_ListObjectVersions_VirtualHosted_ReturnsListObjectVersions()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://my-bucket.s3.us-east-1.amazonaws.com/?versions");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.ListObjectVersions, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
    }

    [Fact]
    public void Classify_UploadPart_VirtualHosted_ReturnsUploadPart()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://my-bucket.s3.us-east-1.amazonaws.com/large.zip?partNumber=3&uploadId=xyz");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.UploadPart, result.Operation);
        Assert.Equal("my-bucket", result.BucketName);
        Assert.Equal("large.zip", result.KeyName);
    }

    [Fact]
    public void Classify_PutObjectTagging_VirtualHosted_ReturnsPutObjectTagging()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://my-bucket.s3.us-east-1.amazonaws.com/file.txt?tagging");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.PutObjectTagging, result.Operation);
    }

    [Fact]
    public void Classify_AbortMultipartUpload_VirtualHosted_ReturnsAbortMultipartUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://my-bucket.s3.us-east-1.amazonaws.com/large.zip?uploadId=abc");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.AbortMultipartUpload, result.Operation);
    }

    [Fact]
    public void Classify_CompleteMultipartUpload_VirtualHosted_ReturnsCompleteMultipartUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://my-bucket.s3.us-east-1.amazonaws.com/large.zip?uploadId=abc");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.CompleteMultipartUpload, result.Operation);
    }

    [Fact]
    public void Classify_DeleteObjectTagging_VirtualHosted_ReturnsDeleteObjectTagging()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://my-bucket.s3.us-east-1.amazonaws.com/file.txt?tagging");

        var result = S3OperationClassifier.Classify(request);

        Assert.Equal(S3Operation.DeleteObjectTagging, result.Operation);
    }
}
