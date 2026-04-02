using TestTrackingDiagrams.Extensions.EfCore.Relational;

namespace TestTrackingDiagrams.Tests.EfCore.Relational;

public class GetDiagramLabelTests
{
    // ──────────────────────────────────────────────────────────
    //  Summarised — operation name string
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(SqlOperation.Select, "Select")]
    [InlineData(SqlOperation.Insert, "Insert")]
    [InlineData(SqlOperation.Update, "Update")]
    [InlineData(SqlOperation.Delete, "Delete")]
    [InlineData(SqlOperation.Merge, "Merge")]
    [InlineData(SqlOperation.Upsert, "Upsert")]
    [InlineData(SqlOperation.StoredProc, "StoredProc")]
    [InlineData(SqlOperation.Other, "Other")]
    public void Summarised_ReturnsOperationName(SqlOperation operation, string expected)
    {
        var info = new SqlOperationInfo(operation, "Users", "SELECT * FROM Users");

        var label = SqlOperationClassifier.GetDiagramLabel(info, SqlTrackingVerbosity.Summarised);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Detailed — operation name string
    // ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(SqlOperation.Select, "Select")]
    [InlineData(SqlOperation.Insert, "Insert")]
    [InlineData(SqlOperation.Update, "Update")]
    [InlineData(SqlOperation.Delete, "Delete")]
    [InlineData(SqlOperation.Merge, "Merge")]
    [InlineData(SqlOperation.Upsert, "Upsert")]
    [InlineData(SqlOperation.StoredProc, "StoredProc")]
    [InlineData(SqlOperation.Other, "Other")]
    public void Detailed_ReturnsOperationName(SqlOperation operation, string expected)
    {
        var info = new SqlOperationInfo(operation, "Users", "SELECT * FROM Users");

        var label = SqlOperationClassifier.GetDiagramLabel(info, SqlTrackingVerbosity.Detailed);

        Assert.Equal(expected, label);
    }

    // ──────────────────────────────────────────────────────────
    //  Raw — returns null (caller uses SQL keyword)
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Raw_ReturnsNull()
    {
        var info = new SqlOperationInfo(SqlOperation.Select, "Users", "SELECT * FROM Users");

        var label = SqlOperationClassifier.GetDiagramLabel(info, SqlTrackingVerbosity.Raw);

        Assert.Null(label);
    }
}
