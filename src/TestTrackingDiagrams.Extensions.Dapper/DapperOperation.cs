namespace TestTrackingDiagrams;

/// <summary>
/// Classified Dapper operation types.
/// </summary>
public enum DapperOperation
{
    Query,
    Insert,
    Update,
    Delete,
    Merge,
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
