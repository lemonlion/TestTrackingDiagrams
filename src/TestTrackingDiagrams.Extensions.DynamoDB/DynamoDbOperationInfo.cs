namespace TestTrackingDiagrams.Extensions.DynamoDB;

public record DynamoDbOperationInfo(
    DynamoDbOperation Operation,
    string? TableName,
    string? StatementText = null);
