namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

/// <summary>
/// The result of classifying a EfCore.Relational operation, containing the operation type and metadata.
/// </summary>
public record SqlOperationInfo(
    SqlOperation Operation,
    string? TableName,
    string? CommandText);
