namespace TestTrackingDiagrams.Extensions.BigQuery;

/// <summary>
/// The result of classifying a BigQuery operation, containing the operation type and metadata.
/// </summary>
public record BigQueryOperationInfo(
    BigQueryOperation Operation,
    string? ResourceType,
    string? ResourceName,
    string? ProjectId,
    string? DatasetId);
