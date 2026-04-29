namespace TestTrackingDiagrams.Extensions.DynamoDB;

/// <summary>
/// Classified DynamoDB operation types.
/// </summary>
public enum DynamoDbOperation
{
    PutItem,
    GetItem,
    UpdateItem,
    DeleteItem,
    Query,
    Scan,
    BatchWriteItem,
    BatchGetItem,
    TransactWriteItems,
    TransactGetItems,
    CreateTable,
    DeleteTable,
    DescribeTable,
    ListTables,
    UpdateTable,
    ExecuteStatement,
    BatchExecuteStatement,
    ExecuteTransaction,
    Other
}
