namespace TestTrackingDiagrams.Extensions.DynamoDB;

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
