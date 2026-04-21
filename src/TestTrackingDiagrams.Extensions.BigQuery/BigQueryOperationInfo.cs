namespace TestTrackingDiagrams.Extensions.BigQuery;

public record BigQueryOperationInfo(
    BigQueryOperation Operation,
    string? ResourceType,
    string? ResourceName,
    string? ProjectId,
    string? DatasetId);
