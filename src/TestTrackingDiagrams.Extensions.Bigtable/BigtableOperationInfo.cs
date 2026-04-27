namespace TestTrackingDiagrams.Extensions.Bigtable;

public record BigtableOperationInfo(
    BigtableOperation Operation,
    string? TableName = null,
    string? RowKey = null,
    int? MutationCount = null);
