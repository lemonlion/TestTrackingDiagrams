namespace TestTrackingDiagrams.Extensions.Elasticsearch;

public record ElasticsearchOperationInfo(
    ElasticsearchOperation Operation,
    string? IndexName,
    string? DocumentId = null);
