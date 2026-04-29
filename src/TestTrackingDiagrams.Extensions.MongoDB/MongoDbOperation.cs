namespace TestTrackingDiagrams.Extensions.MongoDB;

/// <summary>
/// Classified MongoDB operation types.
/// </summary>
public enum MongoDbOperation
{
    Find,
    Insert,
    Update,
    Delete,
    Aggregate,
    Count,
    FindAndModify,
    Distinct,
    BulkWrite,
    CreateIndex,
    DropIndex,
    CreateCollection,
    DropCollection,
    ListCollections,
    ListDatabases,
    GetMore,
    Watch,
    MapReduce,
    CommitTransaction,
    AbortTransaction,
    DropDatabase,
    RenameCollection,
    ListIndexes,
    ServerStatus,
    DbStats,
    CollStats,
    Other
}
