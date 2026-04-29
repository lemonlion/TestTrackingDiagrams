namespace TestTrackingDiagrams;

/// <summary>
/// The result of classifying a Dapper operation, containing the operation type and metadata.
/// </summary>
public record DapperOperationInfo(
    DapperOperation Operation,
    string? TableName,
    string? CommandText = null);
