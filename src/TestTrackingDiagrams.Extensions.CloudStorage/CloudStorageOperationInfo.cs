namespace TestTrackingDiagrams.Extensions.CloudStorage;

/// <summary>
/// The result of classifying a CloudStorage operation, containing the operation type and metadata.
/// </summary>
public record CloudStorageOperationInfo(
    CloudStorageOperation Operation,
    string? BucketName,
    string? ObjectName = null);
