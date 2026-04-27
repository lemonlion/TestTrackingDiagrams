namespace TestTrackingDiagrams.Extensions.MongoDB;

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
