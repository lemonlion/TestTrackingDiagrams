using TestTrackingDiagrams.Extensions.BlobStorage;

namespace TestTrackingDiagrams.Tests.BlobStorage;

public class BlobOperationClassifierTests
{
    // ──────────────────────────────────────────────────────────
    //  Standard blob CRUD operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PutBlob_ReturnsUpload()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Upload, result.Operation);
        Assert.Equal("mycontainer", result.ContainerName);
        Assert.Equal("myblob.txt", result.BlobName);
    }

    [Fact]
    public void Classify_GetBlob_ReturnsDownload()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Download, result.Operation);
        Assert.Equal("mycontainer", result.ContainerName);
        Assert.Equal("myblob.txt", result.BlobName);
    }

    [Fact]
    public void Classify_DeleteBlob_ReturnsDelete()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Delete, result.Operation);
        Assert.Equal("myblob.txt", result.BlobName);
    }

    [Fact]
    public void Classify_HeadBlob_ReturnsGetProperties()
    {
        var request = new HttpRequestMessage(HttpMethod.Head,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.GetProperties, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  Container operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PutContainer_ReturnsCreateContainer()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer?restype=container");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.CreateContainer, result.Operation);
        Assert.Equal("mycontainer", result.ContainerName);
        Assert.Null(result.BlobName);
    }

    [Fact]
    public void Classify_DeleteContainer_ReturnsDeleteContainer()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete,
            "https://account.blob.core.windows.net/mycontainer?restype=container");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.DeleteContainer, result.Operation);
    }

    [Fact]
    public void Classify_ListBlobs_ReturnsListBlobs()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer?restype=container&comp=list");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.ListBlobs, result.Operation);
        Assert.Equal("mycontainer", result.ContainerName);
    }

    // ──────────────────────────────────────────────────────────
    //  Metadata operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PutMetadata_ReturnsSetMetadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=metadata");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.SetMetadata, result.Operation);
    }

    [Fact]
    public void Classify_GetMetadata_ReturnsGetMetadata()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=metadata");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.GetMetadata, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  Block blob operations
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PutBlock_ReturnsPutBlock()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=block&blockid=AAAAAA%3D%3D");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.PutBlock, result.Operation);
    }

    [Fact]
    public void Classify_PutBlockList_ReturnsPutBlockList()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=blocklist");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.PutBlockList, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  Copy and lease
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_CopyBlob_ReturnsCopy()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=copy");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Copy, result.Operation);
    }

    [Fact]
    public void Classify_LeaseBlob_ReturnsLease()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=lease");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Lease, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  Nested blob paths (e.g. virtual directories)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_NestedBlobPath_ParsesCorrectly()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer/folder/subfolder/myblob.json");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Download, result.Operation);
        Assert.Equal("mycontainer", result.ContainerName);
        Assert.Equal("folder/subfolder/myblob.json", result.BlobName);
    }

    // ──────────────────────────────────────────────────────────
    //  Unknown operations → Other
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_UnrecognisedCompParameter_ReturnsOther()
    {
        var request = new HttpRequestMessage(HttpMethod.Put,
            "https://account.blob.core.windows.net/mycontainer/myblob.txt?comp=unknownthing");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_GetContainerWithoutList_ReturnsOther()
    {
        // GET on container with restype=container but no comp=list
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://account.blob.core.windows.net/mycontainer?restype=container");

        var result = BlobOperationClassifier.Classify(request);

        Assert.Equal(BlobOperation.Other, result.Operation);
    }
}
