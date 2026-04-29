namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

/// <summary>
/// The result of classifying a AtlasDataApi operation, containing the operation type and metadata.
/// </summary>
public record AtlasDataApiOperationInfo(
    AtlasDataApiOperation Operation,
    string? DataSource,
    string? DatabaseName,
    string? CollectionName,
    string? FilterText = null);
