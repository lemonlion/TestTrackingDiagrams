namespace TestTrackingDiagrams.Extensions.BlobStorage;

public enum BlobOperation
{
    Upload, Download, Delete, GetProperties, SetMetadata, GetMetadata,
    CreateContainer, DeleteContainer, ListBlobs,
    Copy, PutBlock, PutBlockList, Lease, Other
}
