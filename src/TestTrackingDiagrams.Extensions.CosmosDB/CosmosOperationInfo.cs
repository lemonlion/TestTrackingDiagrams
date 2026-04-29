namespace TestTrackingDiagrams.Extensions.CosmosDB;

/// <summary>
/// The result of classifying a CosmosDB operation, containing the operation type and metadata.
/// </summary>
public record CosmosOperationInfo(
    CosmosOperation Operation,
    string? DatabaseName,
    string? CollectionName,
    string? DocumentId,
    string? QueryText = null);
