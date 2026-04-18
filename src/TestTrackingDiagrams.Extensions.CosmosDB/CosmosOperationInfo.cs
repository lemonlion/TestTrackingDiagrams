namespace TestTrackingDiagrams.Extensions.CosmosDB;

public record CosmosOperationInfo(
    CosmosOperation Operation,
    string? DatabaseName,
    string? CollectionName,
    string? DocumentId,
    string? QueryText = null);
