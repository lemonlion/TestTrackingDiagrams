namespace TestTrackingDiagrams.Extensions.Bigtable;

/// <summary>
/// The result of classifying a Bigtable operation, containing the operation type and metadata.
/// </summary>
public record BigtableOperationInfo(
    BigtableOperation Operation,
    string? TableName = null,
    string? RowKey = null,
    int? MutationCount = null);
