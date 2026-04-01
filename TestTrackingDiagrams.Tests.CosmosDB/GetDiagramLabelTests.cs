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
    //  Detailed — just the operation name (URI carries the detail)
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
    public void Detailed_ReturnsOperationName(CosmosOperation operation, string expected)
    {
        var info = new CosmosOperationInfo(operation, "db", "coll", "doc1");

        var label = CosmosOperationClassifier.GetDiagramLabel(info, CosmosTrackingVerbosity.Detailed);

        Assert.Equal(expected, label);
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
