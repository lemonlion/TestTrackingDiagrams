namespace TestTrackingDiagrams.Extensions.Spanner;

/// <summary>
/// Classified Spanner operation types.
/// </summary>
public enum SpannerOperation
{
    Query,
    Read,
    StreamingRead,
    Insert,
    Update,
    Delete,
    InsertOrUpdate,
    Replace,
    Commit,
    Rollback,
    BeginTransaction,
    BatchDml,
    PartitionQuery,
    PartitionRead,
    Ddl,
    CreateSession,
    DeleteSession,
    Other
}
