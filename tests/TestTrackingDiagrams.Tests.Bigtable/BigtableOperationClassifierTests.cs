using TestTrackingDiagrams.Extensions.Bigtable;

namespace TestTrackingDiagrams.Tests.Bigtable;

public class BigtableOperationClassifierTests
{
    // ─── Classification ─────────────────────────────────────

    [Fact]
    public void Classify_ReadRows_ReturnsReadRows()
    {
        var result = BigtableOperationClassifier.Classify("ReadRows", "projects/p/instances/i/tables/my-table");
        Assert.Equal(BigtableOperation.ReadRows, result.Operation);
    }

    [Fact]
    public void Classify_ReadRowsAsync_ReturnsReadRows()
    {
        var result = BigtableOperationClassifier.Classify("ReadRowsAsync", "projects/p/instances/i/tables/my-table");
        Assert.Equal(BigtableOperation.ReadRows, result.Operation);
    }

    [Fact]
    public void Classify_MutateRow_ReturnsMutateRow()
    {
        var result = BigtableOperationClassifier.Classify("MutateRow", "projects/p/instances/i/tables/my-table", "row1");
        Assert.Equal(BigtableOperation.MutateRow, result.Operation);
    }

    [Fact]
    public void Classify_MutateRows_ReturnsMutateRows()
    {
        var result = BigtableOperationClassifier.Classify("MutateRows", "projects/p/instances/i/tables/my-table", null, 5);
        Assert.Equal(BigtableOperation.MutateRows, result.Operation);
    }

    [Fact]
    public void Classify_CheckAndMutateRow_ReturnsCheckAndMutateRow()
    {
        var result = BigtableOperationClassifier.Classify("CheckAndMutateRow", "projects/p/instances/i/tables/my-table", "row1");
        Assert.Equal(BigtableOperation.CheckAndMutateRow, result.Operation);
    }

    [Fact]
    public void Classify_ReadModifyWriteRow_ReturnsReadModifyWriteRow()
    {
        var result = BigtableOperationClassifier.Classify("ReadModifyWriteRow", "projects/p/instances/i/tables/my-table", "row1");
        Assert.Equal(BigtableOperation.ReadModifyWriteRow, result.Operation);
    }

    [Fact]
    public void Classify_SampleRowKeys_ReturnsSampleRowKeys()
    {
        var result = BigtableOperationClassifier.Classify("SampleRowKeys", "projects/p/instances/i/tables/my-table");
        Assert.Equal(BigtableOperation.SampleRowKeys, result.Operation);
    }

    [Fact]
    public void Classify_Unknown_ReturnsOther()
    {
        var result = BigtableOperationClassifier.Classify("UnknownMethod");
        Assert.Equal(BigtableOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_PreservesTableName()
    {
        var result = BigtableOperationClassifier.Classify("ReadRows", "projects/p/instances/i/tables/my-table");
        Assert.Equal("projects/p/instances/i/tables/my-table", result.TableName);
    }

    [Fact]
    public void Classify_PreservesRowKey()
    {
        var result = BigtableOperationClassifier.Classify("MutateRow", "projects/p/instances/i/tables/t", "row1");
        Assert.Equal("row1", result.RowKey);
    }

    [Fact]
    public void Classify_PreservesMutationCount()
    {
        var result = BigtableOperationClassifier.Classify("MutateRows", "projects/p/instances/i/tables/t", null, 10);
        Assert.Equal(10, result.MutationCount);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_ReadRows_ShowsShortTable()
    {
        var op = new BigtableOperationInfo(BigtableOperation.ReadRows, "projects/p/instances/i/tables/my-table");

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Detailed);

        Assert.Equal("ReadRows ← my-table", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_MutateRow_ShowsShortTable()
    {
        var op = new BigtableOperationInfo(BigtableOperation.MutateRow, "projects/p/instances/i/tables/my-table", "row1");

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Detailed);

        Assert.Equal("MutateRow → my-table", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_MutateRows_ShowsCount()
    {
        var op = new BigtableOperationInfo(BigtableOperation.MutateRows, "projects/p/instances/i/tables/my-table", null, 5);

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Detailed);

        Assert.Equal("MutateRows (×5) → my-table", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_CheckAndMutate_ShowsShortTable()
    {
        var op = new BigtableOperationInfo(BigtableOperation.CheckAndMutateRow, "projects/p/instances/i/tables/my-table", "row1");

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Detailed);

        Assert.Equal("CheckAndMutate → my-table", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_NoTable_ShowsQuestionMark()
    {
        var op = new BigtableOperationInfo(BigtableOperation.ReadRows);

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Detailed);

        Assert.Equal("ReadRows ← ?", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_ReadRows_ReturnsOperationName()
    {
        var op = new BigtableOperationInfo(BigtableOperation.ReadRows, "projects/p/instances/i/tables/t");

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Summarised);

        Assert.Equal("ReadRows", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_MutateRows_CollapsesToMutateRow()
    {
        var op = new BigtableOperationInfo(BigtableOperation.MutateRows, "projects/p/instances/i/tables/t", null, 5);

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Summarised);

        Assert.Equal("MutateRow", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_CheckAndMutateRow_Shortened()
    {
        var op = new BigtableOperationInfo(BigtableOperation.CheckAndMutateRow, "projects/p/instances/i/tables/t", "row1");

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Summarised);

        Assert.Equal("CheckAndMutate", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_IncludesAllInfo()
    {
        var op = new BigtableOperationInfo(BigtableOperation.MutateRow, "projects/p/instances/i/tables/t", "row1");

        var label = BigtableOperationClassifier.GetDiagramLabel(op, BigtableTrackingVerbosity.Raw);

        Assert.Contains("MutateRow", label);
        Assert.Contains("projects/p/instances/i/tables/t", label);
        Assert.Contains("row1", label);
    }
}
