using TestTrackingDiagrams.Extensions.BlobStorage;

namespace TestTrackingDiagrams.Tests.BlobStorage;

public class GetDiagramLabelTests
{
    // ──────────────────────────────────────────────────────────
    //  Summarised — just the operation name
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(BlobOperation.Upload, "Upload")]
    [InlineData(BlobOperation.Download, "Download")]
    [InlineData(BlobOperation.Delete, "Delete")]
    [InlineData(BlobOperation.GetProperties, "GetProperties")]
    [InlineData(BlobOperation.SetMetadata, "SetMetadata")]
    [InlineData(BlobOperation.GetMetadata, "GetMetadata")]
    [InlineData(BlobOperation.CreateContainer, "CreateContainer")]
    [InlineData(BlobOperation.DeleteContainer, "DeleteContainer")]
    [InlineData(BlobOperation.ListBlobs, "ListBlobs")]
    [InlineData(BlobOperation.Copy, "Copy")]
    [InlineData(BlobOperation.PutBlock, "PutBlock")]
    [InlineData(BlobOperation.PutBlockList, "PutBlockList")]
    [InlineData(BlobOperation.Lease, "Lease")]
    [InlineData(BlobOperation.Other, "Other")]
    public void Summarised_ReturnsOperationName(BlobOperation operation, string expected)
    {
        var info = new BlobOperationInfo(operation, "container", "blob.txt");

        var label = BlobOperationClassifier.GetDiagramLabel(info, BlobTrackingVerbosity.Summarised);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Detailed — just the operation name (URI carries the detail)
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(BlobOperation.Upload, "Upload")]
    [InlineData(BlobOperation.Download, "Download")]
    [InlineData(BlobOperation.Delete, "Delete")]
    [InlineData(BlobOperation.GetProperties, "GetProperties")]
    [InlineData(BlobOperation.SetMetadata, "SetMetadata")]
    [InlineData(BlobOperation.GetMetadata, "GetMetadata")]
    [InlineData(BlobOperation.CreateContainer, "CreateContainer")]
    [InlineData(BlobOperation.DeleteContainer, "DeleteContainer")]
    [InlineData(BlobOperation.ListBlobs, "ListBlobs")]
    [InlineData(BlobOperation.Copy, "Copy")]
    [InlineData(BlobOperation.PutBlock, "PutBlock")]
    [InlineData(BlobOperation.PutBlockList, "PutBlockList")]
    [InlineData(BlobOperation.Lease, "Lease")]
    [InlineData(BlobOperation.Other, "Other")]
    public void Detailed_ReturnsOperationName(BlobOperation operation, string expected)
    {
        var info = new BlobOperationInfo(operation, "container", "blob.txt");

        var label = BlobOperationClassifier.GetDiagramLabel(info, BlobTrackingVerbosity.Detailed);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Raw — returns null (caller uses standard method + path)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Raw_ReturnsNull()
    {
        var info = new BlobOperationInfo(BlobOperation.Upload, "container", "blob.txt");

        var label = BlobOperationClassifier.GetDiagramLabel(info, BlobTrackingVerbosity.Raw);

        Assert.Null(label);
    }
}
