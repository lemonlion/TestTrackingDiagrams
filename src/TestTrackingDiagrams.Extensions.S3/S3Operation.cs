namespace TestTrackingDiagrams.Extensions.S3;

public enum S3Operation
{
    PutObject,
    GetObject,
    DeleteObject,
    DeleteObjects,
    HeadObject,
    CopyObject,
    ListObjectsV2,
    ListBuckets,
    CreateBucket,
    DeleteBucket,
    GetBucketLocation,
    CreateMultipartUpload,
    UploadPart,
    CompleteMultipartUpload,
    AbortMultipartUpload,
    PutObjectTagging,
    GetObjectTagging,
    DeleteObjectTagging,
    ListObjectVersions,
    Other
}
