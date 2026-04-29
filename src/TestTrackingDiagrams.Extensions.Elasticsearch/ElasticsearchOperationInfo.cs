namespace TestTrackingDiagrams.Extensions.Elasticsearch;

/// <summary>
/// The result of classifying a Elasticsearch operation, containing the operation type and metadata.
/// </summary>
public record ElasticsearchOperationInfo(
    ElasticsearchOperation Operation,
    string? IndexName,
    string? DocumentId = null);
