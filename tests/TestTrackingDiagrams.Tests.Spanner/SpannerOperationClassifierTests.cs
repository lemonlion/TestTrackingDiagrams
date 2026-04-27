using TestTrackingDiagrams.Extensions.Spanner;

namespace TestTrackingDiagrams.Tests.Spanner;

public class SpannerOperationClassifierTests
{
    // ─── SQL Classification ─────────────────────────────────

    [Fact]
    public void ClassifySql_Select_ReturnsQuery()
    {
        var result = SpannerOperationClassifier.ClassifySql("SELECT * FROM Users WHERE Id = @id");
        Assert.Equal(SpannerOperation.Query, result.Operation);
    }

    [Fact]
    public void ClassifySql_Select_ExtractsTableName()
    {
        var result = SpannerOperationClassifier.ClassifySql("SELECT * FROM Users WHERE Id = @id");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void ClassifySql_Select_BacktickTableName()
    {
        var result = SpannerOperationClassifier.ClassifySql("SELECT * FROM `Users` WHERE Id = @id");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void ClassifySql_Insert_ReturnsInsert()
    {
        var result = SpannerOperationClassifier.ClassifySql("INSERT INTO Users (Id, Name) VALUES (@id, @name)");
        Assert.Equal(SpannerOperation.Insert, result.Operation);
    }

    [Fact]
    public void ClassifySql_Insert_ExtractsTableName()
    {
        var result = SpannerOperationClassifier.ClassifySql("INSERT INTO Users (Id, Name) VALUES (@id, @name)");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void ClassifySql_Update_ReturnsUpdate()
    {
        var result = SpannerOperationClassifier.ClassifySql("UPDATE Users SET Name = @name WHERE Id = @id");
        Assert.Equal(SpannerOperation.Update, result.Operation);
    }

    [Fact]
    public void ClassifySql_Update_ExtractsTableName()
    {
        var result = SpannerOperationClassifier.ClassifySql("UPDATE Users SET Name = @name WHERE Id = @id");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void ClassifySql_Delete_ReturnsDelete()
    {
        var result = SpannerOperationClassifier.ClassifySql("DELETE FROM Users WHERE Id = @id");
        Assert.Equal(SpannerOperation.Delete, result.Operation);
    }

    [Fact]
    public void ClassifySql_Delete_ExtractsTableName()
    {
        var result = SpannerOperationClassifier.ClassifySql("DELETE FROM Users WHERE Id = @id");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void ClassifySql_CreateTable_ReturnsDdl()
    {
        var result = SpannerOperationClassifier.ClassifySql("CREATE TABLE Users (Id INT64) PRIMARY KEY (Id)");
        Assert.Equal(SpannerOperation.Ddl, result.Operation);
    }

    [Fact]
    public void ClassifySql_AlterTable_ReturnsDdl()
    {
        var result = SpannerOperationClassifier.ClassifySql("ALTER TABLE Users ADD COLUMN Email STRING(MAX)");
        Assert.Equal(SpannerOperation.Ddl, result.Operation);
    }

    [Fact]
    public void ClassifySql_DropTable_ReturnsDdl()
    {
        var result = SpannerOperationClassifier.ClassifySql("DROP TABLE Users");
        Assert.Equal(SpannerOperation.Ddl, result.Operation);
    }

    [Fact]
    public void ClassifySql_Empty_ReturnsOther()
    {
        var result = SpannerOperationClassifier.ClassifySql("");
        Assert.Equal(SpannerOperation.Other, result.Operation);
    }

    [Fact]
    public void ClassifySql_Null_ReturnsOther()
    {
        var result = SpannerOperationClassifier.ClassifySql(null);
        Assert.Equal(SpannerOperation.Other, result.Operation);
    }

    [Fact]
    public void ClassifySql_LeadingWhitespace_StillClassifies()
    {
        var result = SpannerOperationClassifier.ClassifySql("   SELECT * FROM Orders");
        Assert.Equal(SpannerOperation.Query, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void ClassifySql_CaseInsensitive()
    {
        var result = SpannerOperationClassifier.ClassifySql("select * from Users");
        Assert.Equal(SpannerOperation.Query, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void ClassifySql_PreservesSqlText()
    {
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var result = SpannerOperationClassifier.ClassifySql(sql);
        Assert.Equal(sql, result.SqlText);
    }

    // ─── gRPC Classification ────────────────────────────────

    [Fact]
    public void ClassifyGrpc_ExecuteSql_ReturnsQuery()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("ExecuteSql", "Users", "projects/p/instances/i/databases/d");
        Assert.Equal(SpannerOperation.Query, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_ExecuteStreamingSql_ReturnsQuery()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("ExecuteStreamingSql");
        Assert.Equal(SpannerOperation.Query, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_Read_ReturnsRead()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("Read", "Users");
        Assert.Equal(SpannerOperation.Read, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_StreamingRead_ReturnsStreamingRead()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("StreamingRead", "Users");
        Assert.Equal(SpannerOperation.StreamingRead, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_Commit_ReturnsCommit()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("Commit");
        Assert.Equal(SpannerOperation.Commit, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_CommitAsync_ReturnsCommit()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("CommitAsync");
        Assert.Equal(SpannerOperation.Commit, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_Rollback_ReturnsRollback()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("Rollback");
        Assert.Equal(SpannerOperation.Rollback, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_BeginTransaction_ReturnsBeginTransaction()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("BeginTransaction");
        Assert.Equal(SpannerOperation.BeginTransaction, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_ExecuteBatchDml_ReturnsBatchDml()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("ExecuteBatchDml");
        Assert.Equal(SpannerOperation.BatchDml, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_PartitionQuery_ReturnsPartitionQuery()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("PartitionQuery");
        Assert.Equal(SpannerOperation.PartitionQuery, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_PartitionRead_ReturnsPartitionRead()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("PartitionRead");
        Assert.Equal(SpannerOperation.PartitionRead, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_CreateSession_ReturnsCreateSession()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("CreateSession");
        Assert.Equal(SpannerOperation.CreateSession, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_BatchCreateSessions_ReturnsCreateSession()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("BatchCreateSessions");
        Assert.Equal(SpannerOperation.CreateSession, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_DeleteSession_ReturnsDeleteSession()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("DeleteSession");
        Assert.Equal(SpannerOperation.DeleteSession, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_Unknown_ReturnsOther()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("UnknownMethod");
        Assert.Equal(SpannerOperation.Other, result.Operation);
    }

    [Fact]
    public void ClassifyGrpc_PreservesTableAndDatabase()
    {
        var result = SpannerOperationClassifier.ClassifyGrpc("Read", "Users", "projects/p/instances/i/databases/d");
        Assert.Equal("Users", result.TableName);
        Assert.Equal("projects/p/instances/i/databases/d", result.DatabaseId);
    }

    // ─── Diagram labels ─────────────────────────────────────

    [Fact]
    public void GetDiagramLabel_Detailed_Query_ShowsTable()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Detailed);
        Assert.Equal("SELECT FROM Users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Insert_ShowsTable()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Insert, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Detailed);
        Assert.Equal("INSERT INTO Users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Update_ShowsTable()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Update, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Detailed);
        Assert.Equal("UPDATE Users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Delete_ShowsTable()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Delete, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Detailed);
        Assert.Equal("DELETE FROM Users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_Read_ShowsTable()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Read, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Detailed);
        Assert.Equal("Read Users", label);
    }

    [Fact]
    public void GetDiagramLabel_Detailed_NoTable_ShowsQuestionMark()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Query);
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Detailed);
        Assert.Equal("SELECT FROM ?", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_Query_ReturnsSelect()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Summarised);
        Assert.Equal("SELECT", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_Insert_ReturnsInsert()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Insert, "Users");
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Summarised);
        Assert.Equal("INSERT", label);
    }

    [Fact]
    public void GetDiagramLabel_Summarised_Commit_ReturnsCommit()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Commit);
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Summarised);
        Assert.Equal("Commit", label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_WithSql_ReturnsSqlText()
    {
        var sql = "SELECT * FROM Users WHERE Id = @id";
        var op = new SpannerOperationInfo(SpannerOperation.Query, "Users", SqlText: sql);
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Raw);
        Assert.Equal(sql, label);
    }

    [Fact]
    public void GetDiagramLabel_Raw_NoSql_FallsBackToOperationName()
    {
        var op = new SpannerOperationInfo(SpannerOperation.Commit);
        var label = SpannerOperationClassifier.GetDiagramLabel(op, SpannerTrackingVerbosity.Raw);
        Assert.Equal("Commit", label);
    }
}
