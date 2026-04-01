using TestTrackingDiagrams.Extensions.CosmosDB;

namespace TestTrackingDiagrams.Tests.CosmosDB;

public class GetDiagramLabelTests
{
    // ──────────────────────────────────────────────────────────
    //  Summarised — just the operation name
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(CosmosOperation.Create, "Create")]
    [InlineData(CosmosOperation.Read, "Read")]
    [InlineData(CosmosOperation.Replace, "Replace")]
    [InlineData(CosmosOperation.Delete, "Delete")]
    [InlineData(CosmosOperation.Upsert, "Upsert")]
    [InlineData(CosmosOperation.Query, "Query")]
    [InlineData(CosmosOperation.Patch, "Patch")]
    [InlineData(CosmosOperation.List, "List")]
    [InlineData(CosmosOperation.ExecStoredProc, "ExecStoredProc")]
    [InlineData(CosmosOperation.Other, "Other")]
    public void Summarised_ReturnsOperationName(CosmosOperation operation, string expected)
    {
        var info = new CosmosOperationInfo(operation, "db", "coll", "doc1");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Summarised);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Detailed — operation with context
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Detailed_Create_WithDocId_ShowsCollectionAndDoc()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Create, "db", "orders", "order-123");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("Create [orders/order-123]", label);
    }

    [Fact]
    public void Detailed_Create_WithoutDocId_ShowsCollectionOnly()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Create, "db", "orders", null);

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("Create [orders]", label);
    }

    [Fact]
    public void Detailed_Read_WithDocId_ShowsCollectionAndDoc()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Read, "db", "orders", "order-456");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("Read [orders/order-456]", label);
    }

    [Fact]
    public void Detailed_Query_ShowsCollectionOnly()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Query, "db", "orders", null, "SELECT * FROM c");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("Query [orders]", label);
    }

    [Fact]
    public void Detailed_List_ShowsCollectionOnly()
    {
        var info = new CosmosOperationInfo(CosmosOperation.List, "db", "orders", null);

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("List [orders]", label);
    }

    [Fact]
    public void Detailed_ExecSproc_ShowsSprocName()
    {
        var info = new CosmosOperationInfo(CosmosOperation.ExecStoredProc, "db", "coll", "bulkDelete");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("ExecSproc [bulkDelete]", label);
    }

    [Fact]
    public void Detailed_Delete_WithDocId_ShowsCollectionAndDoc()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Delete, "db", "orders", "order-789");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("Delete [orders/order-789]", label);
    }

    [Fact]
    public void Detailed_Other_WithNoCollection_ShowsOperationOnly()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Other, "db", null, null);

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal("Other", label);
    }

    // ──────────────────────────────────────────────────────────
    //  Raw — returns null (caller uses standard method + path)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Raw_ReturnsNull()
    {
        var info = new CosmosOperationInfo(CosmosOperation.Create, "db", "orders", "doc1");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Raw);

        Assert.Null(label);
    }
}
