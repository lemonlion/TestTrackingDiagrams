using System.Data;

namespace TestTrackingDiagrams.Tests.Dapper;

public class DapperOperationClassifierTests
{
    // ─── SQL keyword classification ─────────────────────────────

    [Fact]
    public void Select_is_classified_as_Query()
    {
        var result = DapperOperationClassifier.Classify("SELECT * FROM Users WHERE Id = @Id");
        Assert.Equal(DapperOperation.Query, result.Operation);
    }

    [Fact]
    public void Insert_is_classified_as_Insert()
    {
        var result = DapperOperationClassifier.Classify("INSERT INTO Users (Name, Email) VALUES (@Name, @Email)");
        Assert.Equal(DapperOperation.Insert, result.Operation);
    }

    [Fact]
    public void Update_is_classified_as_Update()
    {
        var result = DapperOperationClassifier.Classify("UPDATE Users SET Name = @Name WHERE Id = @Id");
        Assert.Equal(DapperOperation.Update, result.Operation);
    }

    [Fact]
    public void Delete_is_classified_as_Delete()
    {
        var result = DapperOperationClassifier.Classify("DELETE FROM Users WHERE Id = @Id");
        Assert.Equal(DapperOperation.Delete, result.Operation);
    }

    [Fact]
    public void Merge_is_classified_as_Merge()
    {
        var result = DapperOperationClassifier.Classify("MERGE INTO Target USING Source ON ...");
        Assert.Equal(DapperOperation.Merge, result.Operation);
    }

    [Fact]
    public void Exec_is_classified_as_StoredProcedure()
    {
        var result = DapperOperationClassifier.Classify("EXEC sp_GetUsers @Role = 'Admin'");
        Assert.Equal(DapperOperation.StoredProcedure, result.Operation);
    }

    [Fact]
    public void Execute_is_classified_as_StoredProcedure()
    {
        var result = DapperOperationClassifier.Classify("EXECUTE dbo.sp_GetUsers");
        Assert.Equal(DapperOperation.StoredProcedure, result.Operation);
    }

    [Fact]
    public void CommandType_StoredProcedure_overrides_text()
    {
        var result = DapperOperationClassifier.Classify("sp_GetUsers", CommandType.StoredProcedure);
        Assert.Equal(DapperOperation.StoredProcedure, result.Operation);
    }

    [Fact]
    public void Create_table_is_classified()
    {
        var result = DapperOperationClassifier.Classify("CREATE TABLE Users (Id INT PRIMARY KEY)");
        Assert.Equal(DapperOperation.CreateTable, result.Operation);
    }

    [Fact]
    public void Alter_table_is_classified()
    {
        var result = DapperOperationClassifier.Classify("ALTER TABLE Users ADD Email NVARCHAR(200)");
        Assert.Equal(DapperOperation.AlterTable, result.Operation);
    }

    [Fact]
    public void Drop_table_is_classified()
    {
        var result = DapperOperationClassifier.Classify("DROP TABLE Users");
        Assert.Equal(DapperOperation.DropTable, result.Operation);
    }

    [Fact]
    public void Create_index_is_classified()
    {
        var result = DapperOperationClassifier.Classify("CREATE INDEX IX_Users_Name ON Users (Name)");
        Assert.Equal(DapperOperation.CreateIndex, result.Operation);
    }

    [Fact]
    public void Truncate_is_classified()
    {
        var result = DapperOperationClassifier.Classify("TRUNCATE TABLE Users");
        Assert.Equal(DapperOperation.Truncate, result.Operation);
    }

    [Fact]
    public void Begin_transaction_is_classified()
    {
        var result = DapperOperationClassifier.Classify("BEGIN TRANSACTION");
        Assert.Equal(DapperOperation.BeginTransaction, result.Operation);
    }

    [Fact]
    public void Begin_tran_is_classified()
    {
        var result = DapperOperationClassifier.Classify("BEGIN TRAN");
        Assert.Equal(DapperOperation.BeginTransaction, result.Operation);
    }

    [Fact]
    public void Commit_is_classified()
    {
        var result = DapperOperationClassifier.Classify("COMMIT");
        Assert.Equal(DapperOperation.Commit, result.Operation);
    }

    [Fact]
    public void Rollback_is_classified()
    {
        var result = DapperOperationClassifier.Classify("ROLLBACK");
        Assert.Equal(DapperOperation.Rollback, result.Operation);
    }

    [Fact]
    public void Empty_string_is_Other()
    {
        var result = DapperOperationClassifier.Classify("");
        Assert.Equal(DapperOperation.Other, result.Operation);
    }

    [Fact]
    public void Null_is_Other()
    {
        var result = DapperOperationClassifier.Classify(null);
        Assert.Equal(DapperOperation.Other, result.Operation);
    }

    [Fact]
    public void Unknown_sql_is_Other()
    {
        var result = DapperOperationClassifier.Classify("SET NOCOUNT ON");
        Assert.Equal(DapperOperation.Other, result.Operation);
    }

    // ─── Case insensitivity ─────────────────────────────────────

    [Theory]
    [InlineData("select * from Users")]
    [InlineData("Select * From Users")]
    [InlineData("SELECT * FROM Users")]
    public void Classification_is_case_insensitive(string sql)
    {
        var result = DapperOperationClassifier.Classify(sql);
        Assert.Equal(DapperOperation.Query, result.Operation);
    }

    // ─── Table name extraction ──────────────────────────────────

    [Fact]
    public void Extracts_table_from_SELECT()
    {
        var result = DapperOperationClassifier.Classify("SELECT * FROM Users WHERE Id = 1");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Extracts_table_from_bracketed_schema()
    {
        var result = DapperOperationClassifier.Classify("SELECT * FROM [dbo].[Users]");
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Extracts_table_from_INSERT()
    {
        var result = DapperOperationClassifier.Classify("INSERT INTO Orders (ProductId) VALUES (1)");
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Extracts_table_from_UPDATE()
    {
        var result = DapperOperationClassifier.Classify("UPDATE Products SET Price = 9.99");
        Assert.Equal("Products", result.TableName);
    }

    [Fact]
    public void Extracts_table_from_DELETE()
    {
        var result = DapperOperationClassifier.Classify("DELETE FROM Logs WHERE Date < '2024-01-01'");
        Assert.Equal("Logs", result.TableName);
    }

    [Fact]
    public void No_table_for_stored_procedure()
    {
        var result = DapperOperationClassifier.Classify("EXEC sp_GetUsers");
        Assert.Null(result.TableName);
    }

    // ─── GetDiagramLabel ────────────────────────────────────────

    [Fact]
    public void Label_Raw_returns_full_sql()
    {
        var op = new DapperOperationInfo(DapperOperation.Query, "Users", "SELECT * FROM Users");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Raw);
        Assert.Equal("SELECT * FROM Users", label);
    }

    [Fact]
    public void Label_Detailed_includes_table()
    {
        var op = new DapperOperationInfo(DapperOperation.Query, "Users");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Detailed);
        Assert.Equal("SELECT FROM Users", label);
    }

    [Fact]
    public void Label_Detailed_insert_includes_table()
    {
        var op = new DapperOperationInfo(DapperOperation.Insert, "Orders");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Detailed);
        Assert.Equal("INSERT INTO Orders", label);
    }

    [Fact]
    public void Label_Detailed_update_includes_table()
    {
        var op = new DapperOperationInfo(DapperOperation.Update, "Products");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Detailed);
        Assert.Equal("UPDATE Products", label);
    }

    [Fact]
    public void Label_Detailed_delete_includes_table()
    {
        var op = new DapperOperationInfo(DapperOperation.Delete, "Logs");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Detailed);
        Assert.Equal("DELETE FROM Logs", label);
    }

    [Fact]
    public void Label_Detailed_storedproc_extracts_name()
    {
        var op = new DapperOperationInfo(DapperOperation.StoredProcedure, null, "EXEC sp_GetUsers @Role = 'Admin'");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Detailed);
        Assert.Equal("EXEC sp_GetUsers", label);
    }

    [Fact]
    public void Label_Detailed_with_null_table_uses_question_mark()
    {
        var op = new DapperOperationInfo(DapperOperation.Query, null);
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Detailed);
        Assert.Equal("SELECT FROM ?", label);
    }

    [Fact]
    public void Label_Summarised_returns_keyword_only()
    {
        var op = new DapperOperationInfo(DapperOperation.Query, "Users");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Summarised);
        Assert.Equal("SELECT", label);
    }

    [Fact]
    public void Label_Summarised_insert()
    {
        var op = new DapperOperationInfo(DapperOperation.Insert, "Orders");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Summarised);
        Assert.Equal("INSERT", label);
    }

    [Fact]
    public void Label_Summarised_storedproc()
    {
        var op = new DapperOperationInfo(DapperOperation.StoredProcedure, null, "EXEC sp_GetUsers");
        var label = DapperOperationClassifier.GetDiagramLabel(op, DapperTrackingVerbosity.Summarised);
        Assert.Equal("EXEC", label);
    }

    // ─── ExtractProcName ────────────────────────────────────────

    [Fact]
    public void ExtractProcName_from_EXEC_statement()
    {
        var name = DapperOperationClassifier.ExtractProcName("EXEC sp_GetUsers @Role = 'Admin'");
        Assert.Equal("sp_GetUsers", name);
    }

    [Fact]
    public void ExtractProcName_from_EXECUTE_statement()
    {
        var name = DapperOperationClassifier.ExtractProcName("EXECUTE dbo.sp_GetUsers");
        Assert.Equal("dbo.sp_GetUsers", name);
    }

    [Fact]
    public void ExtractProcName_plain_name()
    {
        var name = DapperOperationClassifier.ExtractProcName("sp_GetUsers");
        Assert.Equal("sp_GetUsers", name);
    }

    [Fact]
    public void ExtractProcName_null_returns_question_mark()
    {
        var name = DapperOperationClassifier.ExtractProcName(null);
        Assert.Equal("?", name);
    }

    // ─── Leading whitespace ─────────────────────────────────────

    [Fact]
    public void Leading_whitespace_is_trimmed_before_classification()
    {
        var result = DapperOperationClassifier.Classify("   SELECT * FROM Users");
        Assert.Equal(DapperOperation.Query, result.Operation);
        Assert.Equal("Users", result.TableName);
    }
}
