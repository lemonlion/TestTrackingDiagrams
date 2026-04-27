namespace TestTrackingDiagrams.Extensions.AtlasDataApi;

public record AtlasDataApiOperationInfo(
    AtlasDataApiOperation Operation,
    string? DataSource,
    string? DatabaseName,
    string? CollectionName,
    string? FilterText = null);
