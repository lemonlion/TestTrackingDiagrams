namespace TestTrackingDiagrams.Extensions.BlobStorage;

/// <summary>
/// The result of classifying a BlobStorage operation, containing the operation type and metadata.
/// </summary>
public record BlobOperationInfo(
    BlobOperation Operation,
    string? ContainerName,
    string? BlobName);
