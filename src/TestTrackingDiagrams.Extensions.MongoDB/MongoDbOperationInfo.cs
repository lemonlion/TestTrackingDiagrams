namespace TestTrackingDiagrams.Extensions.MongoDB;

/// <summary>
/// The result of classifying a MongoDB operation, containing the operation type and metadata.
/// </summary>
public record MongoDbOperationInfo(
    MongoDbOperation Operation,
    string? DatabaseName,
    string? CollectionName,
    string? FilterText = null,
    int? DocumentCount = null,
    string? DocumentId = null,
    string? PipelineStages = null,
    bool IsGridFs = false);
