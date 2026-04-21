namespace TestTrackingDiagrams.Extensions.MongoDB;

public record MongoDbOperationInfo(
    MongoDbOperation Operation,
    string? DatabaseName,
    string? CollectionName,
    string? FilterText = null);
