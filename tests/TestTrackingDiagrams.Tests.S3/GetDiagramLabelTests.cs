using TestTrackingDiagrams.Extensions.S3;

namespace TestTrackingDiagrams.Tests.S3;

public class GetDiagramLabelTests
{
    // ──────────────────────────────────────────────────────────
    //  Summarised — just the operation name
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(S3Operation.PutObject, "PutObject")]
    [InlineData(S3Operation.GetObject, "GetObject")]
    [InlineData(S3Operation.DeleteObject, "DeleteObject")]
    [InlineData(S3Operation.DeleteObjects, "DeleteObjects")]
    [InlineData(S3Operation.HeadObject, "HeadObject")]
    [InlineData(S3Operation.CopyObject, "CopyObject")]
    [InlineData(S3Operation.ListObjectsV2, "ListObjectsV2")]
    [InlineData(S3Operation.ListBuckets, "ListBuckets")]
    [InlineData(S3Operation.CreateBucket, "CreateBucket")]
    [InlineData(S3Operation.DeleteBucket, "DeleteBucket")]
    [InlineData(S3Operation.GetBucketLocation, "GetBucketLocation")]
    [InlineData(S3Operation.CreateMultipartUpload, "CreateMultipartUpload")]
    [InlineData(S3Operation.UploadPart, "UploadPart")]
    [InlineData(S3Operation.CompleteMultipartUpload, "CompleteMultipartUpload")]
    [InlineData(S3Operation.AbortMultipartUpload, "AbortMultipartUpload")]
    [InlineData(S3Operation.PutObjectTagging, "PutObjectTagging")]
    [InlineData(S3Operation.GetObjectTagging, "GetObjectTagging")]
    [InlineData(S3Operation.DeleteObjectTagging, "DeleteObjectTagging")]
    [InlineData(S3Operation.ListObjectVersions, "ListObjectVersions")]
    [InlineData(S3Operation.Other, "Other")]
    public void Summarised_ReturnsOperationName(S3Operation operation, string expected)
    {
        var info = new S3OperationInfo(operation, "bucket", "key.txt");

        var label = S3OperationClassifier.GetDiagramLabel(info, S3TrackingVerbosity.Summarised);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Detailed — operation name
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(S3Operation.PutObject, "PutObject")]
    [InlineData(S3Operation.GetObject, "GetObject")]
    [InlineData(S3Operation.DeleteObject, "DeleteObject")]
    [InlineData(S3Operation.DeleteObjects, "DeleteObjects")]
    [InlineData(S3Operation.HeadObject, "HeadObject")]
    [InlineData(S3Operation.CopyObject, "CopyObject")]
    [InlineData(S3Operation.ListObjectsV2, "ListObjectsV2")]
    [InlineData(S3Operation.ListBuckets, "ListBuckets")]
    [InlineData(S3Operation.CreateBucket, "CreateBucket")]
    [InlineData(S3Operation.DeleteBucket, "DeleteBucket")]
    [InlineData(S3Operation.GetBucketLocation, "GetBucketLocation")]
    [InlineData(S3Operation.CreateMultipartUpload, "CreateMultipartUpload")]
    [InlineData(S3Operation.UploadPart, "UploadPart")]
    [InlineData(S3Operation.CompleteMultipartUpload, "CompleteMultipartUpload")]
    [InlineData(S3Operation.AbortMultipartUpload, "AbortMultipartUpload")]
    [InlineData(S3Operation.PutObjectTagging, "PutObjectTagging")]
    [InlineData(S3Operation.GetObjectTagging, "GetObjectTagging")]
    [InlineData(S3Operation.DeleteObjectTagging, "DeleteObjectTagging")]
    [InlineData(S3Operation.ListObjectVersions, "ListObjectVersions")]
    [InlineData(S3Operation.Other, "Other")]
    public void Detailed_ReturnsOperationName(S3Operation operation, string expected)
    {
        var info = new S3OperationInfo(operation, "bucket", "key.txt");

        var label = S3OperationClassifier.GetDiagramLabel(info, S3TrackingVerbosity.Detailed);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Raw — returns null (caller uses standard method + path)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Raw_ReturnsNull()
    {
        var info = new S3OperationInfo(S3Operation.PutObject, "bucket", "key.txt");

        var label = S3OperationClassifier.GetDiagramLabel(info, S3TrackingVerbosity.Raw);

        Assert.Null(label);
    }
}
