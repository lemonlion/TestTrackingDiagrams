namespace TestTrackingDiagrams.Extensions.DynamoDB;

/// <summary>
/// The result of classifying a DynamoDB operation, containing the operation type and metadata.
/// </summary>
public record DynamoDbOperationInfo(
    DynamoDbOperation Operation,
    string? TableName,
    string? StatementText = null);
