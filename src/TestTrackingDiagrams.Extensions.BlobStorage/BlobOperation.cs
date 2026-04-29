namespace TestTrackingDiagrams.Extensions.BlobStorage;

/// <summary>
/// Classified BlobStorage operation types.
/// </summary>
public enum BlobOperation
{
    Upload, Download, Delete, GetProperties, SetMetadata, GetMetadata,
    CreateContainer, DeleteContainer, ListBlobs,
    Copy, PutBlock, PutBlockList, Lease, Other
}
