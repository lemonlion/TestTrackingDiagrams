namespace TestTrackingDiagrams.Extensions.MongoDB;

public record MongoDbOperationInfo(
    MongoDbOperation Operation,
    string? DatabaseName,
    string? CollectionName,
    string? FilterText = null,
    int? DocumentCount = null,
    string? DocumentId = null,
    string? PipelineStages = null,
    bool IsGridFs = false);
