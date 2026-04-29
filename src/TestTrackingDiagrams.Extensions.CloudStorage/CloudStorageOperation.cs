namespace TestTrackingDiagrams.Extensions.CloudStorage;

/// <summary>
/// Classified CloudStorage operation types.
/// </summary>
public enum CloudStorageOperation
{
    Upload,
    Download,
    Delete,
    ListObjects,
    GetMetadata,
    UpdateMetadata,
    Copy,
    Compose,
    CreateBucket,
    DeleteBucket,
    GetBucket,
    ListBuckets,
    Other
}
