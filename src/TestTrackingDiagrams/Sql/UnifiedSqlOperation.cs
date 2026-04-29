namespace TestTrackingDiagrams.Sql;

public enum UnifiedSqlOperation
{
    Select,
    Insert,
    Update,
    Delete,
    Merge,
    Upsert,
    StoredProcedure,
    CreateTable,
    AlterTable,
    DropTable,
    CreateIndex,
    Truncate,
    BeginTransaction,
    Commit,
    Rollback,
    Other
}
