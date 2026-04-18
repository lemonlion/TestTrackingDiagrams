using System.Data;
using TestTrackingDiagrams.Extensions.EfCore.Relational;

namespace TestTrackingDiagrams.Tests.EfCore.Relational;

public class SqlOperationClassifierTests
{
    // ──────────────────────────────────────────────────────────
    //  Core DML — provider-agnostic
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_Select_ReturnsSelect()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM Users");
        Assert.Equal(SqlOperation.Select, result.Operation);
    }

    [Fact]
    public void Classify_InsertInto_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify("INSERT INTO Users (Id, Name) VALUES (1, 'Alice')");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_Update_ReturnsUpdate()
    {
        var result = SqlOperationClassifier.Classify("UPDATE Users SET Name = 'Bob' WHERE Id = 1");
        Assert.Equal(SqlOperation.Update, result.Operation);
    }

    [Fact]
    public void Classify_Delete_ReturnsDelete()
    {
        var result = SqlOperationClassifier.Classify("DELETE FROM Users WHERE Id = 1");
        Assert.Equal(SqlOperation.Delete, result.Operation);
    }

    [Fact]
    public void Classify_LeadingWhitespace_IsIgnored()
    {
        var result = SqlOperationClassifier.Classify("   \n  SELECT * FROM Users");
        Assert.Equal(SqlOperation.Select, result.Operation);
    }

    [Theory]
    [InlineData("select * from Users")]
    [InlineData("Select * From Users")]
    [InlineData("SELECT * FROM Users")]
    public void Classify_CaseInsensitive(string sql)
    {
        var result = SqlOperationClassifier.Classify(sql);
        Assert.Equal(SqlOperation.Select, result.Operation);
    }

    [Fact]
    public void Classify_EmptyString_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_Null_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify(null);
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_UnrecognisedKeyword_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("CREATE TABLE Users (Id INT)");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  CommandType.StoredProcedure
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_StoredProcedureCommandType_WithBareName_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("usp_GetOrders", CommandType.StoredProcedure);
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("usp_GetOrders", result.TableName);
    }

    [Fact]
    public void Classify_StoredProcedureCommandType_WithSchemaPrefix_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("dbo.usp_GetOrders", CommandType.StoredProcedure);
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("usp_GetOrders", result.TableName);
    }

    [Fact]
    public void Classify_TextCommandType_WithSelect_ReturnsSelect()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM Users", CommandType.Text);
        Assert.Equal(SqlOperation.Select, result.Operation);
    }

    [Fact]
    public void Classify_StoredProcedureCommandType_EmptyText_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("", CommandType.StoredProcedure);
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  SQL Server–specific
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_SqlServer_Exec_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("EXEC usp_GetOrders @CustomerId = 1");
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("usp_GetOrders", result.TableName);
    }

    [Fact]
    public void Classify_SqlServer_ExecuteWithBrackets_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("EXECUTE [dbo].[usp_GetOrders] @CustomerId = 1");
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("usp_GetOrders", result.TableName);
    }

    [Fact]
    public void Classify_SqlServer_Merge_ReturnsMerge()
    {
        var result = SqlOperationClassifier.Classify("MERGE [dbo].[Orders] AS target USING @source AS source ON target.Id = source.Id");
        Assert.Equal(SqlOperation.Merge, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_SqlServer_BracketQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT [u].[Id] FROM [dbo].[Users] AS [u]");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_SqlServer_SchemaTable_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("INSERT INTO [inventory].[Products] ([Id]) VALUES (1)");
        Assert.Equal(SqlOperation.Insert, result.Operation);
        Assert.Equal("Products", result.TableName);
    }

    [Fact]
    public void Classify_SqlServer_BulkInsert_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("BULK INSERT Orders FROM 'data.csv'");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_SqlServer_SetNocountThenSelect_ReturnsSelect()
    {
        var result = SqlOperationClassifier.Classify("SET NOCOUNT ON;\nSELECT * FROM Users");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_SqlServer_Truncate_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("TRUNCATE TABLE [Orders]");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    // ──────────────────────────────────────────────────────────
    //  PostgreSQL-specific
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_PostgreSql_Call_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("CALL get_orders()");
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("get_orders", result.TableName);
    }

    [Fact]
    public void Classify_PostgreSql_DoubleQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT \"u\".\"Id\" FROM \"Users\" AS \"u\"");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_PostgreSql_SchemaTable_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM \"public\".\"Orders\"");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_PostgreSql_InsertOnConflictDoUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50) ON CONFLICT (\"Id\") DO UPDATE SET \"Total\" = EXCLUDED.\"Total\"");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_PostgreSql_InsertOnConflictDoNothing_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50) ON CONFLICT (\"Id\") DO NOTHING");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_PostgreSql_Copy_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("COPY Orders FROM '/tmp/data.csv' CSV");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_PostgreSql_Explain_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("EXPLAIN SELECT * FROM Users");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_PostgreSql_CteDelete_ReturnsDelete()
    {
        var result = SqlOperationClassifier.Classify("WITH cte AS (SELECT * FROM \"Expired\") DELETE FROM \"Orders\" WHERE \"Id\" IN (SELECT \"Id\" FROM cte)");
        Assert.Equal(SqlOperation.Delete, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    // ──────────────────────────────────────────────────────────
    //  MySQL-specific
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_MySql_Call_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("CALL `get_orders`()");
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("get_orders", result.TableName);
    }

    [Fact]
    public void Classify_MySql_BacktickQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT `u`.`Id` FROM `Users` AS `u`");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_MySql_DatabaseTable_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("INSERT INTO `mydb`.`Orders` (`Id`) VALUES (1)");
        Assert.Equal(SqlOperation.Insert, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_MySql_InsertOnDuplicateKeyUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO `Orders` (`Id`, `Total`) VALUES (1, 42.50) ON DUPLICATE KEY UPDATE `Total` = VALUES(`Total`)");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_MySql_LoadData_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("LOAD DATA INFILE '/tmp/data.csv' INTO TABLE Orders");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_MySql_CteUpdate_ReturnsUpdate()
    {
        var result = SqlOperationClassifier.Classify("WITH cte AS (SELECT * FROM `Old`) UPDATE `Products` SET `Active` = 0 WHERE `Id` IN (SELECT `Id` FROM cte)");
        Assert.Equal(SqlOperation.Update, result.Operation);
        Assert.Equal("Products", result.TableName);
    }

    // ──────────────────────────────────────────────────────────
    //  SQLite-specific
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_Sqlite_DoubleQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT \"u\".\"Id\" FROM \"Users\"");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_Sqlite_InsertOrReplace_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify("INSERT OR REPLACE INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50)");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
        Assert.Equal("Orders", result.TableName);
    }

    [Fact]
    public void Classify_Sqlite_InsertOrIgnore_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify("INSERT OR IGNORE INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50)");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_Sqlite_InsertOnConflictDoUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50) ON CONFLICT (\"Id\") DO UPDATE SET \"Total\" = excluded.\"Total\"");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
    }

    [Fact]
    public void Classify_Sqlite_InsertOnConflictDoNothing_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50) ON CONFLICT (\"Id\") DO NOTHING");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_Sqlite_CteInsert_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify("WITH cte AS (SELECT * FROM \"Source\") INSERT INTO \"Items\" (\"Id\") SELECT \"Id\" FROM cte");
        Assert.Equal(SqlOperation.Insert, result.Operation);
        Assert.Equal("Items", result.TableName);
    }

    // ──────────────────────────────────────────────────────────
    //  Oracle-specific
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_Oracle_Call_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("CALL get_orders()");
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("get_orders", result.TableName);
    }

    [Fact]
    public void Classify_Oracle_DoubleQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT \"u\".\"ID\" FROM \"USERS\" \"u\"");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("USERS", result.TableName);
    }

    [Fact]
    public void Classify_Oracle_Merge_ReturnsMerge()
    {
        var result = SqlOperationClassifier.Classify("MERGE INTO \"ORDERS\" USING (SELECT * FROM dual) src ON (\"ORDERS\".\"Id\" = src.\"Id\")");
        Assert.Equal(SqlOperation.Merge, result.Operation);
        Assert.Equal("ORDERS", result.TableName);
    }

    [Fact]
    public void Classify_Oracle_SchemaTable_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM \"MYSCHEMA\".\"ORDERS\"");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("ORDERS", result.TableName);
    }

    [Fact]
    public void Classify_Oracle_PlSqlBlock_ReturnsOther()
    {
        var result = SqlOperationClassifier.Classify("BEGIN\n  NULL;\nEND;");
        Assert.Equal(SqlOperation.Other, result.Operation);
    }

    [Fact]
    public void Classify_Oracle_CteMerge_ReturnsMerge()
    {
        var result = SqlOperationClassifier.Classify("WITH cte AS (SELECT * FROM dual) MERGE INTO \"Target\" USING cte src ON (\"Target\".\"Id\" = src.\"Id\")");
        Assert.Equal(SqlOperation.Merge, result.Operation);
        Assert.Equal("Target", result.TableName);
    }

    // ──────────────────────────────────────────────────────────
    //  Spanner GoogleSQL-specific
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_SpannerGoogleSql_Call_ReturnsStoredProc()
    {
        var result = SqlOperationClassifier.Classify("CALL cancel_query(\"12345\")");
        Assert.Equal(SqlOperation.StoredProc, result.Operation);
        Assert.Equal("cancel_query", result.TableName);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_BacktickQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT `u`.`Id` FROM `Users` AS `u`");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_SchemaTable_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM `myschema`.`NewArrivals`");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("NewArrivals", result.TableName);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_Merge_ReturnsMerge()
    {
        var result = SqlOperationClassifier.Classify("MERGE Singers AS target USING temp AS source ON target.SingerId = source.SingerId");
        Assert.Equal(SqlOperation.Merge, result.Operation);
        Assert.Equal("Singers", result.TableName);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_InsertOrUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify("INSERT OR UPDATE INTO Singers (SingerId, Status) VALUES (5, 'inactive')");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
        Assert.Equal("Singers", result.TableName);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_InsertOrIgnore_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify("INSERT OR IGNORE INTO Singers (SingerId, FirstName) VALUES (7, 'Edie')");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_InsertOnConflictDoUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO Singers (SingerId, FirstName) VALUES (1, 'John') ON CONFLICT(SingerId) DO UPDATE SET FirstName = excluded.FirstName");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_InsertOnConflictDoNothing_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO Singers (SingerId, FirstName) VALUES (1, 'John') ON CONFLICT DO NOTHING");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_StatementHint_SkipsHintAndClassifies()
    {
        var result = SqlOperationClassifier.Classify("@{PDML_MAX_PARALLELISM=10} DELETE FROM Singers WHERE true");
        Assert.Equal(SqlOperation.Delete, result.Operation);
        Assert.Equal("Singers", result.TableName);
    }

    [Fact]
    public void Classify_SpannerGoogleSql_CteInsertOrUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify("WITH cte AS (SELECT * FROM Source) INSERT OR UPDATE INTO Target (Id) SELECT Id FROM cte");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
        Assert.Equal("Target", result.TableName);
    }

    // ──────────────────────────────────────────────────────────
    //  Spanner PostgreSQL dialect
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_SpannerPostgreSql_DoubleQuoting_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT \"u\".\"Id\" FROM \"Users\"");
        Assert.Equal(SqlOperation.Select, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    [Fact]
    public void Classify_SpannerPostgreSql_InsertOnConflictDoUpdate_ReturnsUpsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50) ON CONFLICT (\"Id\") DO UPDATE SET \"Total\" = EXCLUDED.\"Total\"");
        Assert.Equal(SqlOperation.Upsert, result.Operation);
    }

    [Fact]
    public void Classify_SpannerPostgreSql_InsertOnConflictDoNothing_ReturnsInsert()
    {
        var result = SqlOperationClassifier.Classify(
            "INSERT INTO \"Orders\" (\"Id\", \"Total\") VALUES (1, 42.50) ON CONFLICT (\"Id\") DO NOTHING");
        Assert.Equal(SqlOperation.Insert, result.Operation);
    }

    [Fact]
    public void Classify_SpannerPostgreSql_DeleteWhereTrue_ReturnsDelete()
    {
        var result = SqlOperationClassifier.Classify("DELETE FROM \"Users\" WHERE true");
        Assert.Equal(SqlOperation.Delete, result.Operation);
        Assert.Equal("Users", result.TableName);
    }

    // ──────────────────────────────────────────────────────────
    //  Table name extraction — edge cases
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_SelectFromUnquoted_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM TableName WHERE Id = 1");
        Assert.Equal("TableName", result.TableName);
    }

    [Fact]
    public void Classify_SelectFromDotted_ExtractsLastPart()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM schema.TableName WHERE Id = 1");
        Assert.Equal("TableName", result.TableName);
    }

    [Fact]
    public void Classify_InsertInto_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("INSERT INTO TableName (col) VALUES (1)");
        Assert.Equal("TableName", result.TableName);
    }

    [Fact]
    public void Classify_UpdateSet_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("UPDATE TableName SET col = 1 WHERE Id = 1");
        Assert.Equal("TableName", result.TableName);
    }

    [Fact]
    public void Classify_DeleteFrom_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("DELETE FROM TableName WHERE Id = 1");
        Assert.Equal("TableName", result.TableName);
    }

    [Fact]
    public void Classify_DeleteWithoutFrom_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("DELETE TableName WHERE Id = 1");
        Assert.Equal("TableName", result.TableName);
    }

    [Fact]
    public void Classify_MergeTarget_ExtractsTableName()
    {
        var result = SqlOperationClassifier.Classify("MERGE TargetTable USING source ON TargetTable.Id = source.Id");
        Assert.Equal("TargetTable", result.TableName);
    }

    [Fact]
    public void Classify_SelectFromSubquery_ReturnsNullTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM (SELECT 1 AS Id) AS sub");
        Assert.Null(result.TableName);
    }

    [Fact]
    public void Classify_SelectConstant_ReturnsNullTableName()
    {
        var result = SqlOperationClassifier.Classify("SELECT 1");
        Assert.Null(result.TableName);
    }

    [Fact]
    public void Classify_MultitableJoin_ReturnsFirstTable()
    {
        var result = SqlOperationClassifier.Classify("SELECT * FROM Users u JOIN Orders o ON u.Id = o.UserId");
        Assert.Equal("Users", result.TableName);
    }
}
